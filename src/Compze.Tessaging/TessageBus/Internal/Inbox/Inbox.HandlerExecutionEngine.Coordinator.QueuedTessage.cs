using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Tessaging.Engine;
using Compze.Tessaging.Engine.Internal;
using Compze.Tessaging.Internal.SystemCE.ThreadingCE;
using Compze.Tessaging.Internal.Transport;
using Compze.Tessaging.TessageBus.Internal.TessageHandling;

namespace Compze.Tessaging.TessageBus.Internal.Inbox;

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
            internal readonly TransportTessage.InComing TransportTessage;
            readonly Coordinator _coordinator;
            readonly Func<object, IUnitOfWorkResolver, Task<object?>> _tessageTask;
            readonly ITaskRunner _taskRunner;
            readonly ITessageStorage _tessageStorage;
            readonly IScopeFactory _scopeFactory;
            readonly TessageHandlerExecutor _executor;

            internal Task<object?> Task => _taskCompletionSource.Task;
            internal TessageId TessageId { get; }

            const string ExecuteTaskName = $"{nameof(HandlerExecutionTask)}_{nameof(Execute)}";

            public void Execute() => _taskRunner.Run(ExecuteTaskName, ExecuteCoreAsync);

            async Task ExecuteCoreAsync()
            {
               try
               {
                  this.Log().Debug($"Handler executing {TransportTessage.TessageTypeEnum} tessage {TessageId}");
                  var tessage = TransportTessage.DeserializeTessageAndCacheForNextCall();
                  await ExecuteTransactionalTessageAsync(tessage).caf();
               }
#pragma warning disable CA1031 // Catch all exception types to ensure _taskCompletionSource is always resolved
               catch(Exception exception)
#pragma warning restore CA1031
               {
                  _taskCompletionSource.TrySetException(exception);
                  _coordinator.Failed(this, exception);
               }
            }

            async Task ExecuteTransactionalTessageAsync(ITessage tessage)
            {
               var retryPolicy = new DefaultRetryPolicy(tessage);

               while(true)
               {
                  var tessageHandlerSucceeded = false;
                  object? result = null;
                  try
                  {
                     using var scope = _scopeFactory.BeginScope();
                     result = await TransactionScopeCe.ExecuteAsync(async () =>
                     {
                        var innerResult = await _tessageTask(tessage, UnitOfWorkResolver.From(scope.Resolver)).caf();
                        await _tessageStorage.MarkAsSucceededAsync(TransportTessage).caf();
                        return innerResult;
                     }).caf();
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

                     await _tessageStorage.RecordExceptionAsync(TransportTessage, exception).caf();

                     if(!retryPolicy.TryAwaitNextRetryTimeForException(exception))
                     {
                        this.Log().Warning(exception, $"Transactional tessage {TessageId} failed after exhausting retries.");
                        await _tessageStorage.MarkAsFailedAsync(TransportTessage).caf();
                        _taskCompletionSource.SetException(exception);
                        _coordinator.Failed(this, exception);
                        return;
                     }

                     this.Log().Warning(exception, $"Transactional tessage {TessageId} failed, will retry.");
                  }
               }
            }

            internal HandlerExecutionTask(TransportTessage.InComing transportTessage, Coordinator coordinator, ITaskRunner taskRunner, ITessageStorage tessageStorage, IScopeFactory scopeFactory, TessageHandlerExecutor executor)
            {
               TessageId = transportTessage.TessageId;
               TransportTessage = transportTessage;
               _coordinator = coordinator;
               _taskRunner = taskRunner;
               _tessageStorage = tessageStorage;
               _scopeFactory = scopeFactory;
               _executor = executor;
               _tessageTask = CreateTessageTask();
            }

            //Refactor: Switching should not be necessary. See also inbox.
            Func<object, IUnitOfWorkResolver, Task<object?>> CreateTessageTask() =>
               TransportTessage.TessageTypeEnum switch
               {
                  TransportTessageType.ExactlyOnceTevent => async (tessage, unitOfWork) =>
                  {
                     //The whole wrapped tevent travels the wire, so a received tevent arrives already wrapped; Wrapped normalizes and passes it through unchanged.
                     await _executor.ExecuteTeventHandlers(PublisherTevent.Wrapped((ITevent)tessage), unitOfWork).caf();
                     return null;
                  },
                  TransportTessageType.ExactlyOnceTommand => async (tessage, unitOfWork) =>
                  {
                     await _executor.ExecuteTommandHandler((IExactlyOnceTommand)tessage, unitOfWork).caf();
                     return unit; //Todo:Properly handle tommands with and without return values
                  },
                  TransportTessageType.BestEffortTevent => throw new ArgumentOutOfRangeException(),
                  _                                     => throw new ArgumentOutOfRangeException()
               };
         }
      }
   }
}
