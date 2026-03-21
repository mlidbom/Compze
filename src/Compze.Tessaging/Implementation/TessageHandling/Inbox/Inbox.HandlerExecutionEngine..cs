using Compze.Abstractions.Tessaging.Hosting.Public;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;

namespace Compze.Tessaging.Implementation.TessageHandling.Inbox;

public partial class Inbox
{
   // ReSharper disable once ArrangeTypeMemberModifiers Resharper is confused. If I remove Internal my code stops compiling.
   public partial class HandlerExecutionEngine(
      ITessagesInFlightTracker globalStateTracker,
      ITessageHandlerRegistry tessagingHandlerRegistry,
      IScopeFactory scopeFactory,
      ITessageStorage storage,
      ITaskRunner taskRunner,
      EndpointId endpointId)
   {
      Thread? _awaitDispatchableTessageThread;

      readonly IReadOnlyList<ITessageDispatchingRule> _dispatchingRules =
      [
         new TommandsAndTeventHandlersDoNotRunInParallelWithEachOtherInTheSameEndpoint()
      ];

      readonly Coordinator _coordinator = new(globalStateTracker, taskRunner, storage, scopeFactory, tessagingHandlerRegistry, endpointId);
      readonly ITaskRunner _taskRunner = taskRunner;

      internal void Enqueue(TransportTessage.InComing transportTessage)
      {
         this.Log().Debug($"Enqueueing {transportTessage.TessageTypeEnum} tessage {transportTessage.TessageId}");
         _coordinator.EnqueueTessageTask(transportTessage);
      }

      void AwaitDispatchableTessageThreadLoop()
      {
         while(true)
         {
            var task = _coordinator.AwaitExecutableHandlerExecutionTask(_dispatchingRules);
            task.Execute();
         }
         // ReSharper disable once FunctionNeverReturns
      }

      internal void Start()
      {
         this.Log().Info("Starting");
         _awaitDispatchableTessageThread = _taskRunner.RunOnNamedThread(
            nameof(AwaitDispatchableTessageThreadLoop),
            AwaitDispatchableTessageThreadLoop,
            ThreadPriority.AboveNormal);
      }

      internal void Stop()
      {
         this.Log().Info("Stopping");
         if(_awaitDispatchableTessageThread != null)
         {
            _awaitDispatchableTessageThread.Interrupt();
            _awaitDispatchableTessageThread.Join();
            _awaitDispatchableTessageThread = null;
         }
         this.Log().Info("Stopped");
      }
   }
}
