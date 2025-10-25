using System;
using System.Threading.Tasks;
using Compze.Abstractions.Tessaging.Public;
using Compze.Tessaging.Implementation.MessageHandling.Abstractions;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE.LinqCE;

namespace Compze.Tessaging.Implementation.MessageHandling;

partial class Inbox
{
   partial class HandlerExecutionEngine
   {
      partial class Coordinator
      {
         // ReSharper disable once MemberCanBePrivate.Local Resharper is just confused....
         internal class HandlerExecutionTask
         {
            readonly TaskCompletionSource<object?> _taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
            internal readonly TransportMessage.InComing TransportMessage;
            readonly Coordinator _coordinator;
            readonly Func<object, object?> _messageTask;
            readonly ITaskRunner _taskRunner;
            readonly IMessageStorage _messageStorage;
            readonly IServiceLocator _serviceLocator;
            readonly IMessageHandlerRegistry _handlerRegistry;

            internal Task<object?> Task => _taskCompletionSource.Task;
            public Guid MessageId { get; }

            const string ExecuteTaskName = $"{nameof(HandlerExecutionTask)}_{nameof(Execute)}";
            public void Execute()
            {
               var message = TransportMessage.DeserializeMessageAndCacheForNextCall();
               _taskRunner.Run(ExecuteTaskName, () =>
               {
                  var retryPolicy = new DefaultRetryPolicy(message);

                  while(true)
                  {
                     try
                     {
                        var result = message is IMustBeHandledTransactionally
                                        ? _serviceLocator.ExecuteTransactionInIsolatedScope(() =>
                                        {
                                           var innerResult = _messageTask(message);
                                           if(message is IAtMostOnceTessage)
                                           {
                                              _messageStorage.MarkAsSucceeded(TransportMessage);
                                           }

                                           return innerResult;
                                        })
                                        : _serviceLocator.ExecuteInIsolatedScope(() => _messageTask(message));

                        _taskCompletionSource.SetResult(result);
                        _coordinator.Succeeded(this);
                        return;
                     }
                     catch(Exception exception)
                     {
                        if(message is IAtMostOnceTessage)
                        {
                           _messageStorage.RecordException(TransportMessage, exception);
                        }

                        if(!retryPolicy.TryAwaitNextRetryTimeForException(exception))
                        {
                           if(message is IAtMostOnceTessage)
                           {
                              _messageStorage.MarkAsFailed(TransportMessage);
                           }

                           _taskCompletionSource.SetException(exception);
                           _coordinator.Failed(this, exception);
                           return;
                        }
                     }
                  }
               });
            }

            public HandlerExecutionTask(TransportMessage.InComing transportMessage, Coordinator coordinator, ITaskRunner taskRunner, IMessageStorage messageStorage, IServiceLocator serviceLocator, IMessageHandlerRegistry handlerRegistry)
            {
               MessageId = transportMessage.MessageId;
               TransportMessage = transportMessage;
               _coordinator = coordinator;
               _taskRunner = taskRunner;
               _messageStorage = messageStorage;
               _serviceLocator = serviceLocator;
               _handlerRegistry = handlerRegistry;
               _messageTask = CreateMessageTask();
            }

            //Refactor: Switching should not be necessary. See also inbox.
            Func<object, object?> CreateMessageTask() =>
               TransportMessage.MessageTypeEnum switch
               {
                  Implementation.TransportMessage.TransportMessageType.ExactlyOnceEvent => message =>
                  {
                     var eventHandlers = _handlerRegistry.GetEventHandlers(message.GetType());
                     eventHandlers.ForEach(handler => handler((IExactlyOnceTevent)message));
                     return null;
                  },
                  Implementation.TransportMessage.TransportMessageType.AtMostOnceCommandWithReturnValue => message =>
                  {
                     var commandHandler = _handlerRegistry.GetCommandHandlerWithReturnValue(message.GetType());
                     return commandHandler((IAtMostOnceHypermediaTommand)message);
                  },
                  Implementation.TransportMessage.TransportMessageType.AtMostOnceCommand => message =>
                  {
                     var commandHandler = _handlerRegistry.GetCommandHandler(message.GetType());
                     commandHandler((IAtMostOnceHypermediaTommand)message);
                     return unit.Value; //Todo:Properly handle commands with and without return values
                  },
                  Implementation.TransportMessage.TransportMessageType.ExactlyOnceCommand => message =>
                  {
                     var commandHandler = _handlerRegistry.GetCommandHandler(message.GetType());
                     commandHandler((IExactlyOnceTommand)message);
                     return unit.Value;//Todo:Properly handle commands with and without return values
                  },
                  Implementation.TransportMessage.TransportMessageType.NonTransactionalQuery => actualMessage =>
                  {
                     var queryHandler = _handlerRegistry.GetQueryHandler(actualMessage.GetType());
                     //todo: Double dispatch instead of casting?
                     return queryHandler((ITuery<object>)actualMessage);
                  },
                  _ => throw new ArgumentOutOfRangeException()
               };
         }
      }
   }
}