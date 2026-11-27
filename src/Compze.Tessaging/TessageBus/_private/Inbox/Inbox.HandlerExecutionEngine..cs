using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.Engine._private;
using Compze.Tessaging._internal.TessagesInFlight;
using Compze.Tessaging._private.SystemCE.ThreadingCE;
using Compze.Threading;
using Compze.Tessaging._private.Transport;

namespace Compze.Tessaging.TessageBus._private.Inbox;

partial class Inbox
{
   // ReSharper disable once ArrangeTypeMemberModifiers Resharper is confused. If I remove Internal my code stops compiling.
   internal partial class HandlerExecutionEngine(
      ITessagesInFlightTracker globalStateTracker,
      TessageHandlerExecutor executor,
      IScopeFactory scopeFactory,
      ITessageStorage storage,
      ITaskRunner taskRunner,
      RetryPolicy retryPolicy,
      EndpointId endpointId) : IDisposable, IAsyncDisposable
   {
      Task? _awaitDispatchableTessageLoop;
      bool _disposed;

      //todo: properly belongs to a per-handler watchdog on the engine, not to shutdown. Until that exists this is the loud, non-hanging backstop.
      ///<summary>How long <see cref="DrainAndStopHandlingAsync"/> waits for the inbox to drain, and then for a running try to<br/>
      /// end, before giving up and letting teardown proceed. Generous: a normal shutdown drains in milliseconds, so reaching<br/>
      /// this means a handler is likely hung</summary>
      static readonly WaitTimeout ShutdownDrainPatience = WaitTimeout.Seconds(30);

      readonly Coordinator _coordinator = new(globalStateTracker, taskRunner, storage, scopeFactory, executor, retryPolicy, endpointId);
      readonly ITaskRunner _taskRunner = taskRunner;

      ///<summary>Enqueues a tessage for handling, behind every tessage enqueued before it. <paramref name="failingTime"/> is how<br/>
      /// long its handling has already been failing: zero for a tessage just received, what the endpoint's previous process<br/>
      /// recorded for one the recovery scan enqueues again.</summary>
      internal void Enqueue(TransportTessage.InComing transportTessage, TimeSpan failingTime)
      {
         this.Log().Debug($"Enqueueing {transportTessage.TessageTypeEnum} tessage {transportTessage.TessageId}");
         _coordinator.EnqueueTessageTask(transportTessage, failingTime);
      }

      ///<summary>Opens the admission of the tessages the endpoint sends itself — see<br/>
      /// <see cref="Coordinator.StartAdmittingTessagesSentToItself"/>.</summary>
      internal void StartAdmittingTessagesSentToItself() => _coordinator.StartAdmittingTessagesSentToItself();

      ///<summary>Begins admitting a tessage the endpoint sends itself — see <see cref="Coordinator.BeginAdmittingATessageSentToItself"/>.</summary>
      internal Coordinator.AdmissionOfATessageSentToItself BeginAdmittingATessageSentToItself() => _coordinator.BeginAdmittingATessageSentToItself();

      ///<summary>Refuses the tessages the endpoint sends itself from now on, and waits for those being admitted — see<br/>
      /// <see cref="Coordinator.StopAdmittingTessagesSentToItselfAsync"/>.</summary>
      internal Task StopAdmittingTessagesSentToItselfAsync() => _coordinator.StopAdmittingTessagesSentToItselfAsync(ShutdownDrainPatience);

      ///<summary>Drains the inbox for shutdown and stops handling — see <see cref="Coordinator.DrainAndStopAsync"/>.</summary>
      internal Task DrainAndStopHandlingAsync(CancellationToken cancellationToken) => _coordinator.DrainAndStopAsync(ShutdownDrainPatience, cancellationToken);

      ///<summary>Stops handling the moment <paramref name="cancellationToken"/> is cancelled — see<br/>
      /// <see cref="Coordinator.StopHandlingWhenCancelled"/>.</summary>
      internal CancellationTokenRegistration StopHandlingWhenCancelled(CancellationToken cancellationToken) => _coordinator.StopHandlingWhenCancelled(cancellationToken);

      void AwaitDispatchableTessageThreadLoop()
      {
         try
         {
            while(true)
            {
               var task = _coordinator.AwaitExecutableHandlerExecutionTask();
               task.Execute();
            }
         }
         catch(OperationCanceledException) {} // Expected: stopping handling cancelled the wait for the next dispatchable tessage.
      }

      internal void Start()
      {
         this.Log().Info("Starting");
         _awaitDispatchableTessageLoop = _taskRunner.RunOnNamedThreadAsync(
            nameof(AwaitDispatchableTessageThreadLoop),
            AwaitDispatchableTessageThreadLoop,
            ThreadPriority.AboveNormal);
      }

      ///<summary>Stops handling and awaits the dispatch loop thread's end, holding no thread while it does; throws a<br/>
      /// <see cref="TimeoutException"/> if the thread takes longer than 5 seconds to end. The inbox and the container both<br/>
      /// dispose the engine: only the first disposal does anything.</summary>
      public async ValueTask DisposeAsync()
      {
         if(_disposed) return;
         _disposed = true;

         await _coordinator.StopAsync().caf();
         //The endpoint's stop refused them already, unless it failed before it could.
         await _coordinator.StopAdmittingTessagesSentToItselfAsync(ShutdownDrainPatience).caf();
         if(_awaitDispatchableTessageLoop != null)
         {
            this.Log().Info("Stopping");
            await _awaitDispatchableTessageLoop.WaitAsync(5.Seconds()).caf();
            _awaitDispatchableTessageLoop = null;
            this.Log().Info("Stopped");
         }
         _coordinator.Dispose();
      }

      ///<summary>The backstop for a container disposed synchronously: the inbox's own disposal has stopped the loop already, so<br/>
      /// this waits only in a composition that disposes the engine without the inbox.</summary>
      public void Dispose()
      {
         //todo:urgent: a synchronous wait on asynchronous work, through WaitUnwrappingException, which VSTHRD002 can't see: it
         //   holds this thread, and stalls while the thread pool is busy. See
         //   dev_docs/TODO/synchronous-paths-that-need-a-free-thread-pool-thread.md (Magnus, 2026-11-24).
         DisposeAsync().AsTask().WaitUnwrappingException();
      }
   }
}
