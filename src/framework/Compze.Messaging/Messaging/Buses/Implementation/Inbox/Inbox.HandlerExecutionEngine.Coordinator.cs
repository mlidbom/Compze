﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Compze.DependencyInjection;
using Compze.SystemCE.ThreadingCE;
using Compze.SystemCE.ThreadingCE.ResourceAccess;
using static Compze.Contracts.Assert;

namespace Compze.Messaging.Buses.Implementation;

partial class Inbox
{
   partial class HandlerExecutionEngine
   {
      //refactor: Consider moving all message type specific responsibilities into the message class or other class. Probably create more subtypes so that no type checking is required. See also inbox.
      partial class Coordinator(IGlobalBusStateTracker globalStateTracker, ITaskRunner taskRunner, IMessageStorage messageStorage, IServiceLocator serviceLocator, IMessageHandlerRegistry messageHandlerRegistry)
      {
         readonly ITaskRunner _taskRunner = taskRunner;
         readonly IMessageStorage _messageStorage = messageStorage;
         readonly IServiceLocator _serviceLocator = serviceLocator;
         readonly IMessageHandlerRegistry _messageHandlerRegistry = messageHandlerRegistry;
         readonly IThreadShared<NonThreadsafeImplementation> _implementation = ThreadShared.WithDefaultTimeout(new NonThreadsafeImplementation(globalStateTracker));

         internal HandlerExecutionTask AwaitExecutableHandlerExecutionTask(IReadOnlyList<IMessageDispatchingRule> dispatchingRules)
         {
            HandlerExecutionTask? handlerExecutionTask = null;
            _implementation.Await(implementation => implementation.TryGetDispatchableMessage(dispatchingRules, out handlerExecutionTask));
            return Result.ReturnNotNull(handlerExecutionTask);
         }

         public Task<object?> EnqueueMessageTask(TransportMessage.InComing message) => _implementation.Update(implementation =>
         {
            var inflightMessage = new HandlerExecutionTask(message, this, _taskRunner, _messageStorage, _serviceLocator, _messageHandlerRegistry);
            implementation.EnqueueMessageTask(inflightMessage);
            return inflightMessage.Task;
         });

         void Succeeded(HandlerExecutionTask queuedMessageInformation) => _implementation.Update(implementation => implementation.Succeeded(queuedMessageInformation));

         void Failed(HandlerExecutionTask queuedMessageInformation, Exception exception) => _implementation.Update(implementation => implementation.Failed(queuedMessageInformation, exception));

         class NonThreadsafeImplementation(IGlobalBusStateTracker globalStateTracker) : IExecutingMessagesSnapshot
         {
            const int MaxConcurrentlyExecutingHandlers = 20;
            readonly IGlobalBusStateTracker _globalStateTracker = globalStateTracker;


            //performance: Split waiting messages into prioritized categories: Exactly once event/command, At most once event/command,  NonTransactional query
            //don't postpone checking if mutations are allowed to run because we have a ton of queries queued up. Also the queries are likely not allowed to run due to the commands and events!
            //performance: Use static type caching trick to ensure that we know which rules need to be applied to which messages. Don't check rules that don't apply. (Double dispatching might be required.)
            public IReadOnlyList<TransportMessage.InComing> AtMostOnceCommands => _executingAtMostOnceCommands;
            public IReadOnlyList<TransportMessage.InComing> ExactlyOnceCommands => _executingExactlyOnceCommands;
            public IReadOnlyList<TransportMessage.InComing> ExactlyOnceEvents => _executingExactlyOnceEvents;
            public IReadOnlyList<TransportMessage.InComing> ExecutingNonTransactionalQueries => _executingNonTransactionalQueries;

            readonly List<HandlerExecutionTask> _messagesWaitingToExecute = [];

            internal bool TryGetDispatchableMessage(IReadOnlyList<IMessageDispatchingRule> dispatchingRules, [NotNullWhen(true)] out HandlerExecutionTask? dispatchable)
            {
               dispatchable = null!;
               if(_executingMessages >= MaxConcurrentlyExecutingHandlers)
               {
                  return false;
               }

               dispatchable = _messagesWaitingToExecute
                 .FirstOrDefault(queuedTask => dispatchingRules.All(rule => rule.CanBeDispatched(this, queuedTask.TransportMessage)));

               if (dispatchable == null)
               {
                  return false;
               }

               Dispatching(dispatchable);
               return true;
            }

            public void EnqueueMessageTask(HandlerExecutionTask message) => _messagesWaitingToExecute.Add(message);

            internal void Succeeded(HandlerExecutionTask queuedMessageInformation) => DoneDispatching(queuedMessageInformation);

            internal void Failed(HandlerExecutionTask queuedMessageInformation, Exception exception) => DoneDispatching(queuedMessageInformation, exception);

            //Refactor: Switching should not be necessary. See also inbox.
            void Dispatching(HandlerExecutionTask dispatchable)
            {
               _executingMessages++;

               switch(dispatchable.TransportMessage.MessageTypeEnum)
               {
                  case TransportMessage.TransportMessageType.ExactlyOnceEvent:
                     _executingExactlyOnceEvents.Add(dispatchable.TransportMessage);
                     break;
                  case TransportMessage.TransportMessageType.AtMostOnceCommandWithReturnValue:
                  case TransportMessage.TransportMessageType.AtMostOnceCommand:
                     _executingAtMostOnceCommands.Add(dispatchable.TransportMessage);
                     break;
                  case TransportMessage.TransportMessageType.ExactlyOnceCommand:
                     _executingExactlyOnceCommands.Add(dispatchable.TransportMessage);
                     break;
                  case TransportMessage.TransportMessageType.NonTransactionalQuery:
                     _executingNonTransactionalQueries.Add(dispatchable.TransportMessage);
                     break;
                  default:
                     throw new ArgumentOutOfRangeException();
               }

               _messagesWaitingToExecute.Remove(dispatchable);
            }

            //Refactor: Switching should not be necessary. See also inbox.
            void DoneDispatching(HandlerExecutionTask doneExecuting, Exception? exception = null)
            {
               _executingMessages--;

               switch(doneExecuting.TransportMessage.MessageTypeEnum)
               {
                  case TransportMessage.TransportMessageType.ExactlyOnceEvent:
                     _executingExactlyOnceEvents.Remove(doneExecuting.TransportMessage);
                     break;
                  case TransportMessage.TransportMessageType.AtMostOnceCommandWithReturnValue:
                  case TransportMessage.TransportMessageType.AtMostOnceCommand:
                     _executingAtMostOnceCommands.Remove(doneExecuting.TransportMessage);
                     break;
                  case TransportMessage.TransportMessageType.ExactlyOnceCommand:
                     _executingExactlyOnceCommands.Remove(doneExecuting.TransportMessage);
                     break;
                  case TransportMessage.TransportMessageType.NonTransactionalQuery:
                     _executingNonTransactionalQueries.Remove(doneExecuting.TransportMessage);
                     break;
                  default:
                     throw new ArgumentOutOfRangeException();
               }

               _globalStateTracker.DoneWith(doneExecuting.MessageId, exception);
            }

            int _executingMessages;
            readonly List<TransportMessage.InComing> _executingExactlyOnceCommands = [];
            readonly List<TransportMessage.InComing> _executingAtMostOnceCommands = [];
            readonly List<TransportMessage.InComing> _executingExactlyOnceEvents = [];
            readonly List<TransportMessage.InComing> _executingNonTransactionalQueries = [];
         }
      }
   }
}