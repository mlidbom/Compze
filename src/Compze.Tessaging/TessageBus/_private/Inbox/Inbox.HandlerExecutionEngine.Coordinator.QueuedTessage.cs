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
            internal readonly TransportTessage.InComing TransportTessage;
            readonly Coordinator _coordinator;
            readonly Func<object, IUnitOfWorkResolver, Task> _tessageTask;
            readonly ITaskRunner _taskRunner;
            readonly ITessageStorage _tessageStorage;
            readonly IScopeFactory _scopeFactory;
            readonly TessageHandlerExecutor _executor;

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
#pragma warning disable CA1031 // Catch all exception types to ensure the coordinator always learns how the execution ended
               catch(Exception exception)
#pragma warning restore CA1031
               {
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
                  try
                  {
                     //A unit of work, so that what the handler's work finds from the transaction alone, such as the taggregate
                     //stores' unit of work, finds this one.
                     await _scopeFactory.ExecuteUnitOfWorkAsync(async unitOfWork =>
                     {
                        //The claim rides this handling transaction, so it and the handler's work commit or roll back as one:
                        //a second execution of the same tessage - whatever produced it - skips before running any handler.
                        if(!await _tessageStorage.TryClaimForHandlingAsync(TransportTessage).caf())
                        {
                           claimed = false;
                           return;
                        }
                        await _tessageTask(tessage, unitOfWork).caf();
                        await _tessageStorage.MarkAsSucceededAsync(TransportTessage).caf();
                     }).caf();

                     if(!claimed)
                     {
                        this.Log().Info($"Skipping tessage {TessageId}: it is not this execution's to handle - its handling already finished, or another live handling execution holds its claim.");
                        _coordinator.Succeeded(this);
                        return;
                     }

                     tessageHandlerSucceeded = true;

                     this.Log().Debug($"Transactional tessage {TessageId} completed successfully");
                     _coordinator.Succeeded(this);
                     return;
                  }
#pragma warning disable CA1031 //A failed execution is retried, or recorded as failed, below: whatever the handler threw.
                  catch(Exception exception)
                  {
#pragma warning restore CA1031
                     if(tessageHandlerSucceeded) //The handler succeeded but something about cleaning up the scope failed.
                     {
                        this.Log().Error(exception, "Tessage handler succeeded but an exception was thrown while cleaning up the scope.");
                        _coordinator.Succeeded(this);
                        return;
                     }

                     await _tessageStorage.RecordExceptionAsync(TransportTessage, exception).caf();

                     if(!retryPolicy.TryAwaitNextRetryTimeForException(exception))
                     {
                        this.Log().Warning(exception, $"Transactional tessage {TessageId} failed after exhausting retries.");
                        await _tessageStorage.MarkAsFailedAsync(TransportTessage).caf();
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
            Func<object, IUnitOfWorkResolver, Task> CreateTessageTask() =>
               TransportTessage.TessageTypeEnum switch
               {
                  //The whole wrapped tevent travels the wire, so a received tevent arrives already wrapped; Wrapped normalizes and passes it through unchanged.
                  TransportTessageType.ExactlyOnceTevent  => (tessage, unitOfWork) => _executor.ExecuteTeventHandlers(PublisherTevent.Wrapped((ITevent)tessage), unitOfWork),
                  TransportTessageType.ExactlyOnceTommand => (tessage, unitOfWork) => _executor.ExecuteTommandHandler((IExactlyOnceTommand)tessage, unitOfWork),
                  TransportTessageType.BestEffortTevent => throw new ArgumentOutOfRangeException(),
                  _                                     => throw new ArgumentOutOfRangeException()
               };
         }
      }
   }
}
