using System.Transactions;
using Compze.Abstractions.Time;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.Exceptions;
using Compze.Tessaging._private.SystemCE.ThreadingCE;
using Compze.Tessaging._internal.SqlLayer;

namespace Compze.Tessaging._private.EndpointCatalog;

static class EndpointProcessLockRegistrar
{
   internal static IComponentRegistrar EndpointProcessLock(this IComponentRegistrar @this) =>
      @this.Register(Singleton.For<EndpointProcessLock>()
                              .CreatedBy((ITessagingSqlLayer.IEndpointCatalogSqlLayer catalog,
                                          EndpointConfiguration endpoint,
                                          IBackgroundExceptionReporter backgroundExceptionReporter)
                                            => new EndpointProcessLock(catalog, endpoint, backgroundExceptionReporter)));
}

///<summary>The endpoint's process lock: the enforcement of "an endpoint runs in exactly one process at a time", held in the<br/>
/// domain database's endpoint catalog. Acquiring registers the endpoint in the catalog (asserting loud that its name and<br/>
/// <see cref="EndpointId"/> are consistent with what the catalog remembers) and takes the lock; releasing frees it for the<br/>
/// endpoint's next process.</summary>
///<remarks>The lock is exclusivity a live holder holds — a database session for the server engines, an OS lock for the<br/>
/// machine-local ones — never a time-bounded lease: no pause, however long, can lose it. So a claim finding the lock held<br/>
/// has proof of a live holder and is refused immediately and loudly<br/>
/// (<see cref="EndpointAlreadyRunningInAnotherProcessException"/>), naming the holder — while a crashed process's lock is<br/>
/// released by the infrastructure, so the endpoint's next process claims it with no waiting and no manual cleanup.<br/>
/// Every catalog act suppresses the ambient transaction: the lock is its own act — enlisting a claim in a business<br/>
/// transaction would defer it to that transaction's commit.</remarks>
class EndpointProcessLock
{
   readonly ITessagingSqlLayer.IEndpointCatalogSqlLayer _catalog;
   readonly EndpointConfiguration _endpoint;
   readonly IBackgroundExceptionReporter _backgroundExceptionReporter;

   ITessagingSqlLayer.IEndpointProcessLockHold? _hold;

   internal EndpointProcessLock(ITessagingSqlLayer.IEndpointCatalogSqlLayer catalog,
                                EndpointConfiguration endpoint,
                                IBackgroundExceptionReporter backgroundExceptionReporter)
   {
      _catalog = catalog;
      _endpoint = endpoint;
      _backgroundExceptionReporter = backgroundExceptionReporter;
   }

   static string LockHolderDescription => $"process {Environment.ProcessId} on {Environment.MachineName}";

   public async Task AcquireAsync() =>
      await TransactionScopeCe.ExecuteAsync(async () =>
      {
         await _catalog.InitAsync().caf();
         await RegisterTheEndpointInTheCatalogAsync().caf();
         //Taking the lock stamps the holder in the same act, so a live lock and its recorded holder are one fact: a process
         //refused here reads this holder's identity, never a blank left by a gap between taking the lock and recording it.
         _hold = await _catalog.TryTakeProcessLockAsync(_endpoint.Name, LockHolderDescription, ReportTheLockLostWhileHeld).caf();
         if(_hold == null) await ThrowRefusedBecauseALiveProcessHoldsTheLockAsync().caf();
      }, TransactionScopeOption.Suppress).caf();

   public async Task ReleaseAsync()
   {
      if(_hold == null) return;
      var hold = _hold;
      _hold = null;

      //Releasing is only ending the lock's session (or mutex): the recorded holder is deliberately left as it was, to be
      //overwritten by the next process's claim. A reader only reads the holder after being refused the lock - so it reads
      //it while the lock is held, never beside this freed one - which is why there is nothing to clear here.
      await hold.DisposeAsync().caf();
   }

   async Task RegisterTheEndpointInTheCatalogAsync()
   {
      var entry = await _catalog.GetEntryByNameAsync(_endpoint.Name).caf();
      if(entry == null)
      {
         await AssertTheEndpointIdIsNotRegisteredUnderAnotherNameAsync().caf();
         if(await _catalog.TryInsertEntryAsync(_endpoint.Name, _endpoint.Id, UtcTimeSource.UtcNow).caf()) return;
         //A racing process created the entry this instant; the identity asserts below still apply to it.
         entry = (await _catalog.GetEntryByNameAsync(_endpoint.Name).caf())._assert().NotNull();
      }

      State.Assert(entry.EndpointId == _endpoint.Id,
                   () => $"The endpoint name '{_endpoint.Name}' is taken in this domain database's endpoint catalog by another endpoint ({entry.EndpointId}). Endpoint names are unique per domain database: the name keys the endpoint's table-set.");
   }

   async Task AssertTheEndpointIdIsNotRegisteredUnderAnotherNameAsync()
   {
      var entryForThisId = await _catalog.GetEntryByEndpointIdAsync(_endpoint.Id).caf();
      State.Assert(entryForThisId == null,
                   () => $"Endpoint id {_endpoint.Id} is registered in this domain database's endpoint catalog under the name '{entryForThisId!.EndpointName}', but this endpoint claims the name '{_endpoint.Name}'. The name keys the endpoint's table-set: renaming an endpoint means decommissioning the old storage, never silently re-keying it.");
   }

   async Task ThrowRefusedBecauseALiveProcessHoldsTheLockAsync()
   {
      var entry = await _catalog.GetEntryByNameAsync(_endpoint.Name).caf();
      //The holder is stamped in the same act as taking the lock, so a live holder reads back non-null - a null here is the one
      //remaining sliver: another process took the lock this instant and has not finished stamping itself yet. Named as that.
      throw new EndpointAlreadyRunningInAnotherProcessException(
         $"Endpoint '{_endpoint.Name}' ({_endpoint.Id}) is already running in another process: its process lock is held by {entry?.LockHolderDescription ?? "a process that took the lock this instant and is still recording itself"}. "
       + "The lock is held by a live process - a dead one's lock is released by the infrastructure - so there is nothing to wait out. "
       + "An endpoint runs in exactly one process at a time; two processes claiming it is a misconfiguration, typically a double deployment.");
   }

   void ReportTheLockLostWhileHeld(Exception sessionDeath) =>
      _backgroundExceptionReporter.ReportException(new EndpointProcessLockSessionLostException(_endpoint, sessionDeath));
}
