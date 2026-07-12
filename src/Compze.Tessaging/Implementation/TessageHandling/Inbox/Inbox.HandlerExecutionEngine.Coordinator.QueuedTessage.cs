using Compze.Contracts;
using Compze.Abstractions.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Teventive.Tevents.Public;

namespace Compze.Tessaging.Implementation.TessageHandling.Inbox;

public partial class Inbox
{
   public partial class HandlerExecutionEngine
   {
      partial class Coordinator
      {
         // ReSharper disable once MemberCanBePrivate.Local Resharper is just confused....
         internal class HandlerExecutionTask
         {
            readonly TaskCompletionSource<object?> _taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
            internal readonly TransportTessage.InComing TransportTessage;
            readonly Coordinator _coordinator;
            readonly Func<object, IScopeResolver, object?> _tessageTask;
            readonly ITaskRunner _taskRunner;
            readonly ITessageStorage _tessageStorage;
            readonly IScopeFactory _scopeFactory;
            readonly ITessageHandlerRegistry _tessagingHandlerRegistry;

            internal Task<object?> Task => _taskCompletionSource.Task;
            internal TessageId TessageId { get; }

            const string ExecuteTaskName = $"{nameof(HandlerExecutionTask)}_{nameof(Execute)}";

            public void Execute() => _taskRunner.Run(ExecuteTaskName, ExecuteCore);

            void ExecuteCore()
            {
               try
               {
                  this.Log().Debug($"Handler executing {TransportTessage.TessageTypeEnum} tessage {TessageId}");
                  var tessage = TransportTessage.DeserializeTessageAndCacheForNextCall();
                  tessage._assert(it => it is IAtMostOnceTessage);
                  ExecuteTransactionalTessage(tessage);
               }
#pragma warning disable CA1031 // Catch all exception types to ensure _taskCompletionSource is always resolved
               catch(Exception exception)
#pragma warning restore CA1031
               {
                  _taskCompletionSource.TrySetException(exception);
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
                     using var scope = _scopeFactory.BeginScope();
                     result = TransactionScopeCe.Execute(() =>
                     {
                        var innerResult = _tessageTask(tessage, scope.Resolver);
                        _tessageStorage.MarkAsSucceeded(TransportTessage);
                        return innerResult;
                     });
                     tessageHandlerSucceeded = true;

                     this.Log().Debug($"Transactional tessage {TessageId} completed successfully");
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
                        this.Log().Warning(exception, $"Transactional tessage {TessageId} failed after exhausting retries.");
                        _tessageStorage.MarkAsFailed(TransportTessage);
                        _taskCompletionSource.SetException(exception);
                        _coordinator.Failed(this, exception);
                        return;
                     }

                     this.Log().Warning(exception, $"Transactional tessage {TessageId} failed, will retry.");
                  }
               }
            }

            internal HandlerExecutionTask(TransportTessage.InComing transportTessage, Coordinator coordinator, ITaskRunner taskRunner, ITessageStorage tessageStorage, IScopeFactory scopeFactory, ITessageHandlerRegistry tessagingHandlerRegistry)
            {
               TessageId = transportTessage.TessageId;
               TransportTessage = transportTessage;
               _coordinator = coordinator;
               _taskRunner = taskRunner;
               _tessageStorage = tessageStorage;
               _scopeFactory = scopeFactory;
               _tessagingHandlerRegistry = tessagingHandlerRegistry;
               _tessageTask = CreateTessageTask();
            }

            //Refactor: Switching should not be necessary. See also inbox.
            Func<object, IScopeResolver, object?> CreateTessageTask() =>
               TransportTessage.TessageTypeEnum switch
               {
                  TransportTessageType.ExactlyOnceTevent => (tessage, kernel) =>
                  {
                     //The wire still carries the inner tevent, so a received tevent is wrapped here before routing. The remote-transport increment puts the wrapper itself on the wire.
                     var wrappedTevent = PublisherIdentifyingTevent.Wrapped((IExactlyOnceTevent)tessage);
                     var teventHandlers = _tessagingHandlerRegistry.GetTeventHandlers(wrappedTevent.GetType());
                     teventHandlers.ForEach(handler => handler(wrappedTevent, kernel));
                     return null;
                  },
                  TransportTessageType.ExactlyOnceTommand => (tessage, kernel) =>
                  {
                     var tommandHandler = _tessagingHandlerRegistry.GetTommandHandler(tessage.GetType());
                     tommandHandler((IExactlyOnceTommand)tessage, kernel);
                     return unit; //Todo:Properly handle tommands with and without return values
                  },
                  _ => throw new ArgumentOutOfRangeException()
               };
         }
      }
   }
}
