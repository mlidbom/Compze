using Compze.Core.Tessaging.Hosting.Public;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.Public;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Typermedia;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Threading;

namespace Compze.Tessaging.Implementation.TessageHandling.Inbox;

public partial class Inbox
{
   // ReSharper disable once ArrangeTypeMemberModifiers Resharper is confused. If I remove Internal my code stops compiling.
   public partial class HandlerExecutionEngine(
      ITessagesInFlightTracker globalStateTracker,
      ITessageHandlerRegistry tessagingHandlerRegistry,
      ITypermediaHandlerRegistry typermediaHandlerRegistry,
      IServiceLocator serviceLocator,
      ITessageStorage storage,
      ITaskRunner taskRunner,
      EndpointId endpointId)
   {
      Thread? _awaitDispatchableTessageThread;

      readonly IReadOnlyList<ITessageDispatchingRule> _dispatchingRules =
      [
         new TueriesExecuteAfterAllTommandsAndTeventsAreDone(),
         new TommandsAndTeventHandlersDoNotRunInParallelWithEachOtherInTheSameEndpoint()
      ];

      readonly Coordinator _coordinator = new(globalStateTracker, taskRunner, storage, serviceLocator, tessagingHandlerRegistry, typermediaHandlerRegistry, endpointId);
      readonly ITaskRunner _taskRunner = taskRunner;

      internal void Enqueue(TransportTessage.InComing transportTessage)
      {
         this.Log().Debug($"Enqueueing {transportTessage.TessageTypeEnum} tessage {transportTessage.TessageId}");
         _coordinator.EnqueueTessageTask(transportTessage);
      }

      public Task<object?> ExecuteAsync(TransportTessage.InComing transportTessage)
      {
         this.Log().Debug($"Executing {transportTessage.TessageTypeEnum} tessage {transportTessage.TessageId}");
         return _coordinator.EnqueueTessageTask(transportTessage);
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
         _awaitDispatchableTessageThread?.InterruptAndJoin();
         _awaitDispatchableTessageThread = null;
         this.Log().Info("Stopped");
      }
   }
}
