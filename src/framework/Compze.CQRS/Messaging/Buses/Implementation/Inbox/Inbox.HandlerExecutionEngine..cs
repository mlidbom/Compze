using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Compze.DependencyInjection;
using Compze.SystemCE.ThreadingCE;

namespace Compze.Messaging.Buses.Implementation;

partial class Inbox
{
   // ReSharper disable once ArrangeTypeMemberModifiers Resharper is confused. If I remove Internal my code stops compiling.
   internal partial class HandlerExecutionEngine(
      IGlobalBusStateTracker globalStateTracker,
      IMessageHandlerRegistry handlerRegistry,
      IServiceLocator serviceLocator,
      IMessageStorage storage,
      ITaskRunner taskRunner)
   {
      Thread? _awaitDispatchableMessageThread;

      readonly IReadOnlyList<IMessageDispatchingRule> _dispatchingRules =
      [
         new QueriesExecuteAfterAllCommandsAndEventsAreDone(),
         new CommandsAndEventHandlersDoNotRunInParallelWithEachOtherInTheSameEndpoint()
      ];

      readonly Coordinator _coordinator = new(globalStateTracker, taskRunner, storage, serviceLocator, handlerRegistry);

      internal Task<object?> Enqueue(TransportMessage.InComing transportMessage) => _coordinator.EnqueueMessageTask(transportMessage);

      void AwaitDispatchableMessageThread()
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
         _awaitDispatchableMessageThread = new Thread(ThreadExceptionHandler.WrapThreadStart(AwaitDispatchableMessageThread))
                                           {
                                              Name = nameof(AwaitDispatchableMessageThread),
                                              Priority = ThreadPriority.AboveNormal
                                           };
         _awaitDispatchableMessageThread.Start();
      }

      public void Stop()
      {
         _awaitDispatchableMessageThread?.InterruptAndJoin();
         _awaitDispatchableMessageThread = null;
      }
   }
}
