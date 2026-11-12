using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Engine._private;
using Compze.Tessaging._internal.TessagesInFlight;
using Compze.Tessaging._private.SystemCE.ThreadingCE;
using Compze.Tessaging.TessageBus._private.TessageHandling.Dispatching;
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
      EndpointId endpointId) : IDisposable, IAsyncDisposable
   {
      Task? _awaitDispatchableTessageLoop;
      readonly CancellationTokenSource _stopping = new();

      //todo: properly belongs to a per-handler watchdog on the engine, not to shutdown. Until that exists this is the loud, non-hanging backstop.
      ///<summary>How long <see cref="AwaitAllReceivedTessagesProcessedAsync"/> waits for the inbox to drain before giving up and letting<br/>
      /// teardown proceed. Generous: a normal shutdown drains in milliseconds, so reaching this means a handler is likely hung</summary>
      static readonly WaitTimeout ShutdownDrainPatience = WaitTimeout.Seconds(30);

      readonly IReadOnlyList<ITessageDispatchingRule> _dispatchingRules =
      [
         new TommandsAndTeventHandlersDoNotRunInParallelWithEachOtherInTheSameEndpoint()
      ];

      readonly Coordinator _coordinator = new(globalStateTracker, taskRunner, storage, scopeFactory, executor, endpointId);
      readonly ITaskRunner _taskRunner = taskRunner;

      internal void Enqueue(TransportTessage.InComing transportTessage)
      {
         this.Log().Debug($"Enqueueing {transportTessage.TessageTypeEnum} tessage {transportTessage.TessageId}");
         _coordinator.EnqueueTessageTask(transportTessage);
      }

      ///<summary>Drains the inbox for shutdown: waits (best-effort, up to <see cref="ShutdownDrainPatience"/>) for every received<br/>
      /// tessage to finish processing, so the endpoint tears down with empty queues.</summary>
      internal Task AwaitAllReceivedTessagesProcessedAsync() => _coordinator.AwaitAllReceivedTessagesProcessedAsync(ShutdownDrainPatience);

      void AwaitDispatchableTessageThreadLoop()
      {
         try
         {
            while(!_stopping.IsCancellationRequested)
            {
               var task = _coordinator.AwaitExecutableHandlerExecutionTask(_dispatchingRules, _stopping.Token);
               task.Execute();
            }
         }
         catch(OperationCanceledException) {} // Expected during shutdown: Dispose() cancelled the wait for the next dispatchable tessage.
      }

      internal void Start()
      {
         this.Log().Info("Starting");
         _awaitDispatchableTessageLoop = _taskRunner.RunOnNamedThread(
            nameof(AwaitDispatchableTessageThreadLoop),
            AwaitDispatchableTessageThreadLoop,
            ThreadPriority.AboveNormal);
      }

      ///<summary>Stops the dispatch loop and awaits its thread's end, holding no thread while it does; throws a<br/>
      /// <see cref="TimeoutException"/> if the thread takes longer than 5 seconds to end.</summary>
      public async ValueTask DisposeAsync()
      {
         if(_awaitDispatchableTessageLoop != null)
         {
            this.Log().Info("Stopping");
            await _stopping.CancelAsync().caf();
            await _awaitDispatchableTessageLoop.WaitAsync(5.Seconds()).caf();
            _awaitDispatchableTessageLoop = null;
            this.Log().Info("Stopped");
         }
         _stopping.Dispose();
      }

      ///<summary>The backstop for a container disposed synchronously: the inbox's own disposal has stopped the loop already, so<br/>
      /// this waits only in a composition that disposes the engine without the inbox.</summary>
      public void Dispose() => DisposeAsync().AsTask().WaitUnwrappingException();
   }
}
