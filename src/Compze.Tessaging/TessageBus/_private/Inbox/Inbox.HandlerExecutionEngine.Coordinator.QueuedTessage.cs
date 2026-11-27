using System.Diagnostics;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Endpoints.ExactlyOnce;
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
         ///<summary>The handling of one received tessage: tried, and tried again while it fails, for as long as the endpoint's<br/>
         /// <see cref="RetryPolicy"/> allow, and then marked failed. Until it is handled or marked failed, it holds its place<br/>
         /// at the head of the line, so that no tessage behind it is handled first.</summary>
         ///<remarks>A try counts only when the endpoint could record it. When it couldn't, such as when the endpoint's database<br/>
         /// is unreachable, it is the endpoint that is failing, not the tessage: the tessage is tried again once the endpoint<br/>
         /// has waited, and neither the try nor the wait counts toward its failing time. Stopping the endpoint's handling ends a wait to<br/>
         /// be tried again, and cancels the token a running try's handlers were handed; a try that fails once handling has<br/>
         /// stopped doesn't count either. The tessage is then left unhandled for the recovery scan at the endpoint's next start.</remarks>
         // ReSharper disable once MemberCanBePrivate.Local Resharper is just confused....
         internal class HandlerExecutionTask
         {
            internal readonly TransportTessage.InComing TransportTessage;
            readonly Coordinator _coordinator;
            readonly Func<object, IUnitOfWorkResolver, CancellationToken, Task> _tessageTask;
            readonly ITaskRunner _taskRunner;
            readonly ITessageStorage _tessageStorage;
            readonly IScopeFactory _scopeFactory;
            readonly TessageHandlerExecutor _executor;
            readonly RetryPolicy _retryPolicy;
            readonly CancellationToken _stopping;
            readonly HandlingRetrySchedule _handlerFailures = new();
            readonly HandlingRetrySchedule _endpointFailures = new();

            ///<summary>How long the tessage's handling has been failing, up to its last recorded failed try, this endpoint's<br/>
            /// earlier processes included.</summary>
            TimeSpan _failingTime;

            ///<summary>When the time that counts toward <see cref="_failingTime"/> began: when this execution began, when its<br/>
            /// last failed try was recorded, or when a wait for the endpoint ended. A <see cref="Stopwatch"/> timestamp.</summary>
            long _failingClockStartedAt;

            ///<summary>True once the next try is the one made as the tessage's retries run out: after it fails, the tessage is<br/>
            /// marked failed, however close to the limit the clock then reads. A wait ends a little early or late by the<br/>
            /// stopwatch, and the last try is decided, not measured.</summary>
            bool _theNextTryIsTheLast;

            internal TessageId TessageId { get; }

            const string ExecuteTaskName = $"{nameof(HandlerExecutionTask)}_{nameof(Execute)}";

            public void Execute() => _taskRunner.Run(ExecuteTaskName, ExecuteCoreAsync);

            async Task ExecuteCoreAsync()
            {
               _failingClockStartedAt = Stopwatch.GetTimestamp();
               try
               {
                  await HandleTryingAgainWhileItFailsAsync().caf();
               }
               catch(OperationCanceledException) when(_stopping.IsCancellationRequested)
               {
                  this.Log().Info($"Leaving tessage {TessageId} unhandled: the endpoint stopped handling while the tessage was being tried, or waited to be tried again. The recovery scan at the endpoint's next start enqueues it again, ahead of the tessages behind it.");
                  _coordinator.LeftUnhandledAtStop(this);
               }
            }

            async Task HandleTryingAgainWhileItFailsAsync()
            {
               while(true)
               {
                  _stopping.ThrowIfCancellationRequested();

                  if(await TryHandleAsync().caf() is not {} failure)
                  {
                     _coordinator.Succeeded(this);
                     return;
                  }

                  //A try that fails once handling has stopped is the endpoint stopping, not the tessage failing: its handlers were
                  //handed the stopping token, cancelled, and gave up. So the try doesn't count, and the tessage is left unhandled
                  //for the next start.
                  _stopping.ThrowIfCancellationRequested();

                  if(!await TryRecordFailedTryAsync(failure).caf())
                  {
                     await WaitForTheEndpointAsync().caf();
                     continue;
                  }

                  if(_theNextTryIsTheLast || _retryPolicy.GivesUpAfterFailingFor(_failingTime))
                  {
                     await MarkFailedAsync(failure).caf();
                     return;
                  }

                  await WaitBeforeTryingAgainAsync(failure).caf();
               }
            }

            ///<summary>One try: a unit of work that claims the tessage, runs its handlers and marks it succeeded, all of which<br/>
            /// commit or roll back as one.</summary>
            ///<returns>Null when the tessage was handled, or was not this execution's to handle; otherwise what the try failed with.</returns>
            async Task<Exception?> TryHandleAsync()
            {
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
                     await _tessageTask(TransportTessage.DeserializeTessageAndCacheForNextCall(), unitOfWork, _stopping).caf();
                     await _tessageStorage.MarkAsSucceededAsync(TransportTessage).caf();
                  }).caf();
               }
#pragma warning disable CA1031 //Whatever a try fails with, the tessage is tried again or marked failed: see HandleTryingAgainWhileItFailsAsync.
               catch(Exception exception)
#pragma warning restore CA1031
               {
                  return exception;
               }

               if(claimed) this.Log().Debug($"Transactional tessage {TessageId} completed successfully");
               else this.Log().Info($"Skipping tessage {TessageId}: it is not this execution's to handle - its handling already finished, or another live handling execution holds its claim.");
               return null;
            }

            ///<summary>Records the failed try, adding the time since <see cref="_failingClockStartedAt"/> to the tessage's<br/>
            /// failing time. False when the endpoint couldn't record it: then the try doesn't count.</summary>
            async Task<bool> TryRecordFailedTryAsync(Exception failure)
            {
               var failingTime = _failingTime + Stopwatch.GetElapsedTime(_failingClockStartedAt);
               try
               {
                  await _tessageStorage.RecordFailedTryAsync(TransportTessage, failure, failingTime).caf();
               }
#pragma warning disable CA1031 //Whatever recording fails with, it is the endpoint that is failing: logged, and the tessage is tried again once the endpoint has waited.
               catch(Exception recordingFailure)
#pragma warning restore CA1031
               {
                  this.Log().Error(recordingFailure, $"The endpoint could not record a failed try of tessage {TessageId}, which failed with: {failure.Message} It is the endpoint that is failing, not the tessage, so the try doesn't count: the tessage is tried again once the endpoint has waited.");
                  return false;
               }

               _failingTime = failingTime;
               _failingClockStartedAt = Stopwatch.GetTimestamp();
               _endpointFailures.Succeeded();
               _coordinator.RecordedFailedTry(this, failure);
               return true;
            }

            ///<summary>Waits while the endpoint itself is failing. The wait doesn't count toward the tessage's failing time.</summary>
            async Task WaitForTheEndpointAsync()
            {
               await Task.Delay(_endpointFailures.DelayAfterAnotherFailure(), _stopping).caf();
               _failingClockStartedAt = Stopwatch.GetTimestamp();
            }

            async Task WaitBeforeTryingAgainAsync(Exception failure)
            {
               var delay = _handlerFailures.DelayAfterAnotherFailure();
               if(_retryPolicy.TimeLeftAfterFailingFor(_failingTime) is {} timeLeft && timeLeft <= delay)
               {
                  delay = timeLeft;
                  _theNextTryIsTheLast = true;
               }

               this.Log().Warning(failure, $"Handling tessage {TessageId} failed. It is tried again in {delay}: its handling has failed for {_failingTime}, and the endpoint's retry policy is {_retryPolicy}.");
               _coordinator.StartedWaitingToBeTriedAgain();
               try
               {
                  await Task.Delay(delay, _stopping).caf();
               }
               finally
               {
                  _coordinator.StoppedWaitingToBeTriedAgain();
               }
            }

            ///<summary>Marks the tessage failed, trying again for as long as the endpoint can't record it, and lets the endpoint go<br/>
            /// on with the tessages behind it.</summary>
            async Task MarkFailedAsync(Exception lastFailure)
            {
               while(!await TryMarkFailedAsync().caf())
               {
                  await Task.Delay(_endpointFailures.DelayAfterAnotherFailure(), _stopping).caf();
               }

               this.Log().Error(lastFailure, $"Marked tessage {TessageId} failed: its handling failed for {_failingTime}, and the endpoint's retry policy is {_retryPolicy}. It isn't handled again until it is retried, and the endpoint goes on with the tessages behind it.");
               _coordinator.Failed(this, lastFailure);
            }

            async Task<bool> TryMarkFailedAsync()
            {
               try
               {
                  await _tessageStorage.MarkAsFailedAsync(TransportTessage).caf();
                  return true;
               }
#pragma warning disable CA1031 //Whatever marking fails with, it is the endpoint that is failing: logged, and marking is tried again once the endpoint has waited.
               catch(Exception markingFailure)
#pragma warning restore CA1031
               {
                  this.Log().Error(markingFailure, $"The endpoint could not mark tessage {TessageId} failed. It tries again once it has waited.");
                  return false;
               }
            }

            internal HandlerExecutionTask(TransportTessage.InComing transportTessage, TimeSpan failingTime, Coordinator coordinator, ITaskRunner taskRunner, ITessageStorage tessageStorage, IScopeFactory scopeFactory, TessageHandlerExecutor executor, RetryPolicy retryPolicy, CancellationToken stopping)
            {
               TessageId = transportTessage.TessageId;
               TransportTessage = transportTessage;
               _failingTime = failingTime;
               _coordinator = coordinator;
               _taskRunner = taskRunner;
               _tessageStorage = tessageStorage;
               _scopeFactory = scopeFactory;
               _executor = executor;
               _retryPolicy = retryPolicy;
               _stopping = stopping;
               _tessageTask = CreateTessageTask();
            }

            //Refactor: Switching should not be necessary. See also inbox.
            Func<object, IUnitOfWorkResolver, CancellationToken, Task> CreateTessageTask() =>
               TransportTessage.TessageTypeEnum switch
               {
                  //The whole wrapped tevent travels the wire, so a received tevent arrives already wrapped; Wrapped normalizes and passes it through unchanged.
                  TransportTessageType.ExactlyOnceTevent  => (tessage, unitOfWork, cancellationToken) => _executor.ExecuteTeventHandlersAsync(PublisherTevent.Wrapped((ITevent)tessage), unitOfWork, cancellationToken),
                  TransportTessageType.ExactlyOnceTommand => (tessage, unitOfWork, cancellationToken) => _executor.ExecuteTommandHandlerAsync((IExactlyOnceTommand)tessage, unitOfWork, cancellationToken),
                  TransportTessageType.BestEffortTevent => throw new ArgumentOutOfRangeException(),
                  _                                     => throw new ArgumentOutOfRangeException()
               };
         }
      }
   }
}
