using Compze.Abstractions.Hosting.Public;
using Compze.Tessaging.Engine;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ThreadingCE;
using Compze.Threading;
using Compze.Tessaging.Implementation.Abstractions;

namespace Compze.Tessaging.Implementation.TessageHandling.Inbox;

public partial class Inbox
{
   // ReSharper disable once ArrangeTypeMemberModifiers Resharper is confused. If I remove Internal my code stops compiling.
   internal partial class HandlerExecutionEngine(
      ITessagesInFlightTracker globalStateTracker,
      TessageHandlerExecutor executor,
      IScopeFactory scopeFactory,
      ITessageStorage storage,
      ITaskRunner taskRunner,
      EndpointId endpointId) : IDisposable
   {
      Thread? _awaitDispatchableTessageThread;
      readonly CancellationTokenSource _stopping = new();

      //todo: properly belongs to a per-handler watchdog on the engine, not to shutdown. Until that exists this is the loud, non-hanging backstop.
      ///<summary>How long <see cref="AwaitAllReceivedTessagesProcessed"/> waits for the inbox to drain before giving up and letting<br/>
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
      internal void AwaitAllReceivedTessagesProcessed() => _coordinator.AwaitAllReceivedTessagesProcessed(ShutdownDrainPatience);

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
         _awaitDispatchableTessageThread = _taskRunner.RunOnNamedThread(
            nameof(AwaitDispatchableTessageThreadLoop),
            AwaitDispatchableTessageThreadLoop,
            ThreadPriority.AboveNormal);
      }

      public void Dispose()
      {
         if(_awaitDispatchableTessageThread != null)
         {
            this.Log().Info("Stopping");
            _stopping.Cancel();
            _awaitDispatchableTessageThread.JoinCE(5.Seconds());
            _awaitDispatchableTessageThread = null;
            this.Log().Info("Stopped");
         }
         _stopping.Dispose();
      }
   }
}
