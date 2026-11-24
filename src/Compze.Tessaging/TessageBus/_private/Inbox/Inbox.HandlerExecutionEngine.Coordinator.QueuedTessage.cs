using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Engine._private;
using Compze.Tessaging._private.SystemCE.ThreadingCE;
using Compze.Tessaging.TessageBus._private.TessageHandling;
using Compze.Tessaging.TessageTypes;
using Compze.Tessaging._private.Transport;

namespace Compze.Tessaging.TessageBus._private.Inbox;

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
                  var claimed = true;
                  object? result = null;
                  try
                  {
                     //A unit of work, so that what the handler's work finds from the transaction alone, such as the taggregate
                     //stores' unit of work, finds this one.
                     result = await _scopeFactory.ExecuteUnitOfWorkAsync(async unitOfWork =>
                     {
                        //The claim rides this handling transaction, so it and the handler's work commit or roll back as one:
                        //a second execution of the same tessage - whatever produced it - skips before running any handler.
                        if(!await _tessageStorage.TryClaimForHandlingAsync(TransportTessage).caf())
                        {
                           claimed = false;
                           return null;
                        }
                        var innerResult = await _tessageTask(tessage, unitOfWork).caf();
                        await _tessageStorage.MarkAsSucceededAsync(TransportTessage).caf();
                        return innerResult;
                     }).caf();

                     if(!claimed)
                     {
                        this.Log().Info($"Skipping tessage {TessageId}: it is not this execution's to handle - its handling already finished, or another live handling execution holds its claim.");
                        _taskCompletionSource.SetResult(null);
                        _coordinator.Succeeded(this);
                        return;
                     }

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

                     //bug: An exactly-once tessage whose handler throws five times in a row is given up on for good. DefaultRetryPolicy allows
                     //   five tries, run at once with no delay between them, so any failure that outlasts them exhausts them all. MarkAsFailedAsync
                     //   then records the tessage as failed: nothing handles it again, not even after a restart, and nothing shows it or retries it.
                     //   The intended behavior, which IAtLeastOnceTessage's documentation and the dev_docs state, is retrying until handled. (website-and-docs, 2026-11-24)
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
