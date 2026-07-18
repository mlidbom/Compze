using Compze.Abstractions.Hosting.Public;
using Compze.Tessaging.Engine;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ThreadingCE;
using Compze.Tessaging.Implementation.Abstractions;

namespace Compze.Tessaging.Implementation.TessageHandling.Inbox;

public partial class Inbox
{
   // ReSharper disable once ArrangeTypeMemberModifiers Resharper is confused. If I remove Internal my code stops compiling.
   public partial class HandlerExecutionEngine(
      ITessagesInFlightTracker globalStateTracker,
      TessageHandlerExecutor executor,
      IScopeFactory scopeFactory,
      ITessageStorage storage,
      ITaskRunner taskRunner,
      EndpointId endpointId) : IDisposable
   {
      Thread? _awaitDispatchableTessageThread;
      readonly CancellationTokenSource _stopping = new();

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
         catch(OperationCanceledException) {} // Expected during shutdown: Stop() cancelled the wait for the next dispatchable tessage.
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
