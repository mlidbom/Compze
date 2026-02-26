using System;
using System.Threading.Tasks;
using Compze.Contracts;
using Compze.Core.Public;
using Compze.Core.Tessaging.Public;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Functional;
using Compze.Utilities.Logging;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.SystemCE.TransactionsCE;

namespace Compze.Tessaging.Implementation.TessageHandling.Inbox;

public partial class Inbox
{
   public partial class HandlerExecutionEngine
   {
      public partial class Coordinator
      {
         // ReSharper disable once MemberCanBePrivate.Local Resharper is just confused....
         public class HandlerExecutionTask
         {
            readonly TaskCompletionSource<object?> _taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
            internal readonly TransportTessage.InComing TransportTessage;
            readonly Coordinator _coordinator;
            readonly Func<object, object?> _tessageTask;
            readonly ITaskRunner _taskRunner;
            readonly ITessageStorage _tessageStorage;
            readonly IServiceLocator _serviceLocator;
            readonly ITessageHandlerRegistry _handlerRegistry;

            public Task<object?> Task => _taskCompletionSource.Task;
            public TessageId TessageId { get; }

            const string ExecuteTaskName = $"{nameof(HandlerExecutionTask)}_{nameof(Execute)}";

            public void Execute() => _taskRunner.Run(ExecuteTaskName, ExecuteCore);

            void ExecuteCore()
            {
               try
               {
                  var tessage = TransportTessage.DeserializeTessageAndCacheForNextCall();

                  if(TransportTessage.TessageTypeEnum == TransportTessageType.TyperMediaTuery)
                     ExecuteTuery(tessage);
                  else
                  {
                     tessage._assert(it => it is IAtMostOnceTessage);
                     ExecuteTransactionalTessage(tessage);
                  }
               }
#pragma warning disable CA1031 // Catch all exception types to ensure _taskCompletionSource is always resolved
               catch(Exception exception)
#pragma warning restore CA1031
               {
                  _taskCompletionSource.TrySetException(exception);
                  _coordinator.Failed(this, exception);
               }
            }

            void ExecuteTuery(ITessage tessage)
            {
               try
               {
                  var result = _serviceLocator.ExecuteInIsolatedScope(() => _tessageTask(tessage));
                  _taskCompletionSource.SetResult(result);
                  _coordinator.Succeeded(this);
               }
#pragma warning disable CA1031 //This is how you handle exceptions when manually using _taskCompletionSource
               catch(Exception exception)
#pragma warning restore CA1031
               {
                  _taskCompletionSource.SetException(exception);
                  _coordinator.Failed(this, exception);
               }
            }

            void ExecuteTransactionalTessage(ITessage tessage)
            {
               var retryPolicy = new DefaultRetryPolicy(tessage);

               while(true)
               {
                  var tessageHandlerSucceeded = false;
                  object? result = null;
                  try
                  {
                     using(_serviceLocator.BeginScope())
                     {
                        result = TransactionScopeCe.Execute(() =>
                        {
                           var innerResult = _tessageTask(tessage);
                           _tessageStorage.MarkAsSucceeded(TransportTessage);
                           return innerResult;
                        });
                        tessageHandlerSucceeded = true;
                     }

                     _taskCompletionSource.SetResult(result);
                     _coordinator.Succeeded(this);
                     return;
                  }
#pragma warning disable CA1031 //This is how you handle exceptions when manually using _taskCompletionSource
                  catch(Exception exception)
                  {
#pragma warning restore CA1031
                     if(tessageHandlerSucceeded) //The handler succeeded but something about cleaning up the scope failed.
                     {
                        this.Log().Error(exception, "Tessage handler succeeded but an exception was thrown while cleaning up the scope.");
                        _taskCompletionSource.SetResult(result);
                        _coordinator.Succeeded(this);
                        return;
                     }

                     _tessageStorage.RecordException(TransportTessage, exception);

                     if(!retryPolicy.TryAwaitNextRetryTimeForException(exception))
                     {
                        this.Log().Error(exception, $"Transactional tessage {TessageId} failed after exhausting retries.");
                        _tessageStorage.MarkAsFailed(TransportTessage);
                        _taskCompletionSource.SetException(exception);
                        _coordinator.Failed(this, exception);
                        return;
                     }

                     this.Log().Warning(exception, $"Transactional tessage {TessageId} failed, will retry.");
                  }
               }
            }

            public HandlerExecutionTask(TransportTessage.InComing transportTessage, Coordinator coordinator, ITaskRunner taskRunner, ITessageStorage tessageStorage, IServiceLocator serviceLocator, ITessageHandlerRegistry handlerRegistry)
            {
               TessageId = transportTessage.TessageId;
               TransportTessage = transportTessage;
               _coordinator = coordinator;
               _taskRunner = taskRunner;
               _tessageStorage = tessageStorage;
               _serviceLocator = serviceLocator;
               _handlerRegistry = handlerRegistry;
               _tessageTask = CreateTessageTask();
            }

            //Refactor: Switching should not be necessary. See also inbox.
            Func<object, object?> CreateTessageTask() =>
               TransportTessage.TessageTypeEnum switch
               {
                  TransportTessageType.ExactlyOnceTevent => tessage =>
                  {
                     var teventHandlers = _handlerRegistry.GetTeventHandlers(tessage.GetType());
                     teventHandlers.ForEach(handler => handler((IExactlyOnceTevent)tessage));
                     return null;
                  },
                  TransportTessageType.TypermediaAtMostOnceTommandWithReturnValue => tessage =>
                  {
                     var tommandHandler = _handlerRegistry.GetTommandHandlerWithReturnValue(tessage.GetType());
                     return tommandHandler((IAtMostOnceTypermediaTommand)tessage);
                  },
                  TransportTessageType.TypermediaAtMostOnceTommand => tessage =>
                  {
                     var tommandHandler = _handlerRegistry.GetTommandHandler(tessage.GetType());
                     tommandHandler((IAtMostOnceTypermediaTommand)tessage);
                     return unit.Value; //Todo:Properly handle tommands with and without return values
                  },
                  TransportTessageType.ExactlyOnceTommand => tessage =>
                  {
                     var tommandHandler = _handlerRegistry.GetTommandHandler(tessage.GetType());
                     tommandHandler((IExactlyOnceTommand)tessage);
                     return unit.Value; //Todo:Properly handle tommands with and without return values
                  },
                  TransportTessageType.TyperMediaTuery => actualTessage =>
                  {
                     var tueryHandler = _handlerRegistry.GetTueryHandler(actualTessage.GetType());
                     //todo: Double dispatch instead of casting?
                     return tueryHandler((ITuery<object>)actualTessage);
                  },
                  _ => throw new ArgumentOutOfRangeException()
               };
         }
      }
   }
}
