using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Logging;
using Compze.Utilities.SystemCE.ThreadingCE;

namespace Compze.Tessaging.Implementation.TessageHandling.Inbox;

public partial class Inbox
{
   // ReSharper disable once ArrangeTypeMemberModifiers Resharper is confused. If I remove Internal my code stops compiling.
   public partial class HandlerExecutionEngine(
      ITessagesInFlightTracker globalStateTracker,
      ITessageHandlerRegistry handlerRegistry,
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

      readonly Coordinator _coordinator = new(globalStateTracker, taskRunner, storage, serviceLocator, handlerRegistry, endpointId);
      readonly ITaskRunner _taskRunner = taskRunner;

      public void Enqueue(TransportTessage.InComing transportTessage)
      {
         this.Log().Debug($"HandlerExecutionEngine enqueueing {transportTessage.TessageTypeEnum} tessage {transportTessage.TessageId}");
         _coordinator.EnqueueTessageTask(transportTessage);
      }

      public Task<object?> ExecuteAsync(TransportTessage.InComing transportTessage)
      {
         this.Log().Debug($"HandlerExecutionEngine executing {transportTessage.TessageTypeEnum} tessage {transportTessage.TessageId}");
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

      public void Start()
      {
         this.Log().Info("HandlerExecutionEngine starting");
         _awaitDispatchableTessageThread = _taskRunner.RunOnNamedThread(
            nameof(AwaitDispatchableTessageThreadLoop),
            AwaitDispatchableTessageThreadLoop,
            ThreadPriority.AboveNormal);
      }

      public void Stop()
      {
         this.Log().Info("HandlerExecutionEngine stopping");
         _awaitDispatchableTessageThread?.InterruptAndJoin();
         _awaitDispatchableTessageThread = null;
         this.Log().Info("HandlerExecutionEngine stopped");
      }
   }
}
