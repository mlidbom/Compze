using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Compze.Abstractions.Tessaging.Transport.Internal;
using Compze.Tessaging.Implementation.MessageHandling.Abstractions;
using Compze.Tessaging.Implementation.MessageHandling.Dispatching;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Threading;

namespace Compze.Tessaging.Implementation.MessageHandling;

partial class Inbox
{
   // ReSharper disable once ArrangeTypeMemberModifiers Resharper is confused. If I remove Internal my code stops compiling.
   internal partial class HandlerExecutionEngine(
      IMessagesInFlightTracker globalStateTracker,
      IMessageHandlerRegistry handlerRegistry,
      IServiceLocator serviceLocator,
      IMessageStorage storage,
      ITaskRunner taskRunner,
      EndpointId endpointId)
   {
      Thread? _awaitDispatchableMessageThread;

      readonly IReadOnlyList<IMessageDispatchingRule> _dispatchingRules =
      [
         new QueriesExecuteAfterAllCommandsAndEventsAreDone(),
         new CommandsAndEventHandlersDoNotRunInParallelWithEachOtherInTheSameEndpoint()
      ];

      readonly Coordinator _coordinator = new(globalStateTracker, taskRunner, storage, serviceLocator, handlerRegistry, endpointId);

      internal Task<object?> Enqueue(TransportMessage.InComing transportMessage) => _coordinator.EnqueueMessageTask(transportMessage);

      void AwaitDispatchableMessageThreadLoop()
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
         _awaitDispatchableMessageThread = taskRunner.RunOnNamedThread(
            nameof(AwaitDispatchableMessageThreadLoop),
            AwaitDispatchableMessageThreadLoop,
            ThreadPriority.AboveNormal);
      }

      public void Stop()
      {
         _awaitDispatchableMessageThread?.InterruptAndJoin();
         _awaitDispatchableMessageThread = null;
      }
   }
}
