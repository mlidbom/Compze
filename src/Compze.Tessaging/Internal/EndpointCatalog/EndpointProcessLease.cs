using System.Transactions;
using Compze.Abstractions.Time.Public;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.Exceptions;
using Compze.Tessaging.Internal.SystemCE.ThreadingCE;
using Compze.Tessaging.Internal.SqlLayer;

namespace Compze.Tessaging.Internal.EndpointCatalog;

static class EndpointProcessLeaseRegistrar
{
   internal static IComponentRegistrar EndpointProcessLease(this IComponentRegistrar @this) =>
      @this.Register(Singleton.For<EndpointProcessLease>()
                              .CreatedBy((ITessagingSqlLayer.IEndpointCatalogSqlLayer catalog,
                                          EndpointConfiguration endpoint,
                                          ProcessLeaseDuration leaseDuration,
                                          IBackgroundExceptionReporter backgroundExceptionReporter)
                                            => new EndpointProcessLease(catalog, endpoint, leaseDuration, backgroundExceptionReporter)));
}

///<summary>The endpoint's process lease: the enforcement of "an endpoint runs in exactly one process at a time", held in the<br/>
/// domain database's endpoint catalog. Acquiring registers the endpoint in the catalog (asserting loud that its name and<br/>
/// <see cref="EndpointId"/> are consistent with what the catalog remembers) and takes the lease; while held, a background<br/>
/// loop heartbeats it; releasing frees it for the endpoint's next process.</summary>
///<remarks>A claimant finding the lease held does not fail immediately: a lease whose holder crashed stays fresh-looking for<br/>
/// up to one <see cref="ProcessLeaseDuration"/>, so the claimant waits that long — heartbeats arriving throughout prove the<br/>
/// holder alive, and only then does the claim fail loud (<see cref="EndpointAlreadyRunningInAnotherProcessException"/>),<br/>
/// naming the holder. A stale lease is taken over silently: that is crash recovery, not conflict.<br/>
/// Every catalog act suppresses the ambient transaction: the lease is its own act — enlisting a claim or a heartbeat in a<br/>
/// business transaction would defer it to that transaction's commit.</remarks>
class EndpointProcessLease
{
   readonly ITessagingSqlLayer.IEndpointCatalogSqlLayer _catalog;
   readonly EndpointConfiguration _endpoint;
   readonly ProcessLeaseDuration _leaseDuration;
   readonly IBackgroundExceptionReporter _backgroundExceptionReporter;

   readonly Guid _leaseHolderId = Guid.NewGuid();
   readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);
   Task? _heartbeatLoop;
   bool _held;

   internal EndpointProcessLease(ITessagingSqlLayer.IEndpointCatalogSqlLayer catalog,
                                 EndpointConfiguration endpoint,
                                 ProcessLeaseDuration leaseDuration,
                                 IBackgroundExceptionReporter backgroundExceptionReporter)
   {
      _catalog = catalog;
      _endpoint = endpoint;
      _leaseDuration = leaseDuration;
      _backgroundExceptionReporter = backgroundExceptionReporter;
   }

   static string LeaseHolderDescription => $"process {Environment.ProcessId} on {Environment.MachineName}";

   public async Task AcquireAsync()
   {
      await TransactionScopeCe.ExecuteAsync(async () =>
      {
         await _catalog.InitAsync().caf();
         await ClaimWithinPatienceAsync().caf();
      }, TransactionScopeOption.Suppress).caf();

      _held = true;
      _heartbeatLoop = TaskCE.Run(HeartbeatUntilReleasedAsync);
   }

   public async Task ReleaseAsync()
   {
      if(!_held) return;
      _held = false;

      _released.TrySetResult();
      await _heartbeatLoop!.caf();
      await TransactionScopeCe.ExecuteAsync(() => _catalog.ReleaseTheLeaseAsync(_endpoint.Name, _leaseHolderId), TransactionScopeOption.Suppress).caf();
   }

   async Task ClaimWithinPatienceAsync()
   {
      var deadline = DateTime.UtcNow + _leaseDuration.ClaimPatience;
      while(true)
      {
         var entry = await _catalog.GetEntryByNameAsync(_endpoint.Name).caf();
         if(entry == null)
         {
            await AssertTheEndpointIdIsNotRegisteredUnderAnotherNameAsync().caf();
            if(await _catalog.TryInsertEntryHoldingTheLeaseAsync(_endpoint.Name, _endpoint.Id, _leaseHolderId, LeaseHolderDescription, UtcTimeSource.UtcNow).caf())
               return;
         }
         else
         {
            State.Assert(entry.EndpointId == _endpoint.Id,
                         () => $"The endpoint name '{_endpoint.Name}' is taken in this domain database's endpoint catalog by another endpoint ({entry.EndpointId}). Endpoint names are unique per domain database: the name keys the endpoint's table-set.");
            if(await _catalog.TryTakeTheLeaseAsync(_endpoint.Name, _leaseHolderId, LeaseHolderDescription, UtcTimeSource.UtcNow, staleBefore: UtcTimeSource.UtcNow - _leaseDuration.Duration).caf())
               return;
         }

         //After the claim attempts above: the last attempt happens at or after the deadline, and the failure describes the
         //same entry whose claim just failed - a re-read could see the lease just-freed and blame a holder that is gone.
         if(DateTime.UtcNow >= deadline)
            throw new EndpointAlreadyRunningInAnotherProcessException(
               $"Endpoint '{_endpoint.Name}' ({_endpoint.Id}) is already running in another process: its process lease is held by {entry?.LeaseHolderDescription ?? "a process that claimed it this instant"}"
             + $"{(entry?.LeaseHeartbeatUtc is {} heartbeat ? $", last heartbeat {(UtcTimeSource.UtcNow - heartbeat).TotalSeconds:0.###}s ago" : "")}. "
             + $"Waited {_leaseDuration.ClaimPatience.TotalSeconds:0.###}s - a dead holder's lease goes stale within that - so the holder is alive. "
             + "An endpoint runs in exactly one process at a time; two processes claiming it is a misconfiguration, typically a double deployment.");

         await Task.Delay(_leaseDuration.HeartbeatInterval).caf();
      }
   }

   async Task AssertTheEndpointIdIsNotRegisteredUnderAnotherNameAsync()
   {
      var entryForThisId = await _catalog.GetEntryByEndpointIdAsync(_endpoint.Id).caf();
      State.Assert(entryForThisId == null,
                   () => $"Endpoint id {_endpoint.Id} is registered in this domain database's endpoint catalog under the name '{entryForThisId!.EndpointName}', but this endpoint claims the name '{_endpoint.Name}'. The name keys the endpoint's table-set: renaming an endpoint means decommissioning the old storage, never silently re-keying it.");
   }

   async Task HeartbeatUntilReleasedAsync()
   {
      while(true)
      {
         if(await Task.WhenAny(_released.Task, Task.Delay(_leaseDuration.HeartbeatInterval)).caf() == _released.Task)
            return;

         var renewed = await TransactionScopeCe.SuppressAmbientAsync(() => _catalog.TryHeartbeatAsync(_endpoint.Name, _leaseHolderId, UtcTimeSource.UtcNow)).caf();
         if(renewed) continue;

         _backgroundExceptionReporter.ReportException(new EndpointAlreadyRunningInAnotherProcessException(
            $"The process lease for endpoint '{_endpoint.Name}' ({_endpoint.Id}) was taken from this live process: its heartbeats went unrefreshed past the lease duration ({_leaseDuration.Duration.TotalSeconds:0.###}s) - a long pause such as a debugger or a machine sleep - and a claimant presumed it dead. Two processes may now be running the endpoint."));
         return;
      }
   }
}
