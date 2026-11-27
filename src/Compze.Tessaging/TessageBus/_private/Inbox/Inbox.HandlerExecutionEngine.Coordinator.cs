using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using System.Diagnostics.CodeAnalysis;
using Compze.Contracts;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.Engine._private;
using Compze.Tessaging._internal.TessagesInFlight;
using Compze.Tessaging._private.SystemCE.ThreadingCE;
using Compze.Threading;
using Compze.Threading.ResourceAccess;
using Compze.Tessaging._private.Transport;

namespace Compze.Tessaging.TessageBus._private.Inbox;

partial class Inbox
{
   partial class HandlerExecutionEngine
   {
      //refactor: Consider moving all tessage type specific responsibilities into the tessage class or other class. Probably create more subtypes so that no type checking is required. See also inbox.
      // ReSharper disable once ArrangeTypeMemberModifiers ReSharper is confused: a nested class is private without it, and the engine hands out an AdmissionOfATessageSentToItself.
      internal partial class Coordinator(ITessagesInFlightTracker globalStateTracker, ITaskRunner taskRunner, ITessageStorage tessageStorage, IScopeFactory scopeFactory, TessageHandlerExecutor executor, RetryPolicy retryPolicy, EndpointId endpointId) : IDisposable
      {
         readonly ITaskRunner _taskRunner = taskRunner;
         readonly ITessageStorage _tessageStorage = tessageStorage;
         readonly IScopeFactory _scopeFactory = scopeFactory;
         readonly TessageHandlerExecutor _executor = executor;
         readonly RetryPolicy _retryPolicy = retryPolicy;
         readonly IAwaitableThreadShared<NonThreadsafeImplementation> _implementation = IAwaitableThreadShared.New(new NonThreadsafeImplementation(globalStateTracker, endpointId));

         ///<summary>Cancelled when handling stops: it ends the wait for the next dispatchable tessage, and every wait of a<br/>
         /// tessage to be tried again.</summary>
         readonly CancellationTokenSource _stopping = new();

         ///<summary>Waits for the next tessage that may be dispatched, and dispatches it; throws an<br/>
         /// <see cref="OperationCanceledException"/> once handling has stopped (<see cref="StopAsync"/>).</summary>
         internal HandlerExecutionTask AwaitExecutableHandlerExecutionTask()
         {
            HandlerExecutionTask? handlerExecutionTask = null;
            _implementation.Await(implementation => implementation.TryGetDispatchableTessage(out handlerExecutionTask), _stopping.Token);
            return handlerExecutionTask._assert().NotNull();
         }

         ///<summary>The shutdown drain, then the stop. Waits until nothing more will be handled before stopping<br/>
         /// (<see cref="NonThreadsafeImplementation.IsDrained"/>), or until <paramref name="cancellationToken"/> is cancelled, then<br/>
         /// stops handling (<see cref="StopAsync"/>), which cancels the token the running tries' handlers were handed, and waits<br/>
         /// for those tries to end, so that this endpoint's process handles nothing after it returns. A tessage that waits to be<br/>
         /// tried again, one whose try ended as handling stopped, and those behind them, are left unhandled, for the recovery scan<br/>
         /// at the endpoint's next start. Best-effort: if the drain lasts longer than <paramref name="patience"/> (a handler is<br/>
         /// likely hung), or the running tries do once cancelled, it logs loudly and returns, leaving teardown to proceed rather<br/>
         /// than hang. Called after the transport has stopped, so nothing new arrives.</summary>
         internal async Task DrainAndStopAsync(WaitTimeout patience, CancellationToken cancellationToken)
         {
            //What the endpoint sends itself from here on waits for its next start, so that the drain ends: a handler the drain
            //runs may send the next step of a flow, and that step's handler the next.
            _implementation.Update(implementation => implementation.StopTakingTessagesSentToItselfIntoTheLine());
            var aHandlerIsLikelyHung = !await TryDrainAsync(patience, cancellationToken).caf();

            await StopAsync().caf();

            if(!aHandlerIsLikelyHung && !await _implementation.TryAwaitAsync(implementation => implementation.ExecutingCount == 0, timeout: patience).caf())
               this.Log().Warning($"Shutdown drain: a try of a tessage's handling was still running {patience} after handling stopped. Proceeding with teardown - a handler is likely hung.");
         }

         ///<summary>Waits until nothing more will be handled before stopping, until <paramref name="cancellationToken"/> is cancelled,<br/>
         /// or for <paramref name="patience"/>.</summary>
         ///<returns>False when <paramref name="patience"/> ran out first: a handler is then likely hung.</returns>
         async Task<bool> TryDrainAsync(WaitTimeout patience, CancellationToken cancellationToken)
         {
            try
            {
               if(await _implementation.TryAwaitAsync(implementation => implementation.IsDrained, cancellationToken, patience).caf()) return true;
            }
            catch(OperationCanceledException) when(cancellationToken.IsCancellationRequested)
            {
               this.Log().Info(_implementation.Read(implementation =>
                  $"Shutdown drain: the stop's token ended the drain (waiting to execute: {implementation.WaitingCount}, executing: {implementation.ExecutingCount}). The tries still running are cancelled, and what they and the tessages behind them didn't finish waits for the endpoint's next start."));
               return true;
            }

            this.Log().Warning(_implementation.Read(implementation =>
               $"Shutdown drain: the inbox did not finish processing every received tessage within {patience} (waiting to execute: {implementation.WaitingCount}, executing: {implementation.ExecutingCount}). Proceeding with teardown - a handler is likely hung."));
            return false;
         }

         ///<summary>Stops handling: nothing more is dispatched, and every wait of a tessage to be tried again ends, leaving the<br/>
         /// tessage unhandled for the recovery scan at the endpoint's next start. Calling it again does nothing more.</summary>
         internal async Task StopAsync()
         {
            _implementation.Update(implementation => implementation.StopDispatching());
            await _stopping.CancelAsync().caf();
         }

         ///<summary>Stops handling (see <see cref="StopAsync"/>) the moment <paramref name="cancellationToken"/> is cancelled, or at<br/>
         /// once if it already is. Dispose the registration before disposing this coordinator.</summary>
         internal CancellationTokenRegistration StopHandlingWhenCancelled(CancellationToken cancellationToken) =>
            cancellationToken.Register(() =>
            {
               _implementation.Update(implementation => implementation.StopDispatching());
               _stopping.Cancel();
            });

         internal void EnqueueTessageTask(TransportTessage.InComing tessage, TimeSpan failingTime) => _implementation.Update(implementation =>
         {
            this.Log().Debug($"Enqueueing {tessage.TessageTypeEnum} tessage {tessage.TessageId}");
            implementation.EnqueueTessageTask(NewHandlerExecutionTask(tessage, failingTime, sentByTheEndpointToItself: false));
         });

         ///<summary>Opens the admission of the tessages the endpoint sends itself: once the recovery scan has enqueued what the<br/>
         /// endpoint's previous process left unhandled, which must have its place in the line first, being older.</summary>
         internal void StartAdmittingTessagesSentToItself() => _implementation.Update(implementation => implementation.StartAdmittingTessagesSentToItself());

         ///<summary>Begins admitting a tessage the endpoint sends itself — see <see cref="AdmissionOfATessageSentToItself"/>. Throws<br/>
         /// before the admission opens (<see cref="StartAdmittingTessagesSentToItself"/>), and once it has closed<br/>
         /// (<see cref="StopAdmittingTessagesSentToItselfAsync"/>).</summary>
         internal AdmissionOfATessageSentToItself BeginAdmittingATessageSentToItself()
         {
            _implementation.Update(implementation => implementation.BeginAdmittingATessageSentToItself());
            return new AdmissionOfATessageSentToItself(this);
         }

         ///<summary>Refuses the tessages the endpoint sends itself from now on, and waits, for at most <paramref name="patience"/>,<br/>
         /// for the senders' transactions of those being admitted to end.</summary>
         internal async Task StopAdmittingTessagesSentToItselfAsync(WaitTimeout patience)
         {
            _implementation.Update(implementation => implementation.StopAdmittingTessagesSentToItself());
            if(!await _implementation.TryAwaitAsync(implementation => implementation.AdmissionsOfTessagesSentToItselfInProgress == 0, timeout: patience).caf())
               this.Log().Warning(_implementation.Read(implementation =>
                  $"Stopping: {implementation.AdmissionsOfTessagesSentToItselfInProgress} transaction(s) sending a tessage to the endpoint itself had still not ended {patience} after the endpoint stopped admitting them. Proceeding with the stop: such a tessage that commits now may wait in the inbox until a later start of the endpoint, behind tessages sent after it."));
         }

         HandlerExecutionTask NewHandlerExecutionTask(TransportTessage.InComing tessage, TimeSpan failingTime, bool sentByTheEndpointToItself) =>
            new(tessage, failingTime, sentByTheEndpointToItself, this, _taskRunner, _tessageStorage, _scopeFactory, _executor, _retryPolicy, _stopping.Token);

         void Succeeded(HandlerExecutionTask queuedTessageInformation) => _implementation.Update(implementation => implementation.Succeeded(queuedTessageInformation));

         void Failed(HandlerExecutionTask queuedTessageInformation, Exception exception) => _implementation.Update(implementation => implementation.Failed(queuedTessageInformation, exception));

         void LeftUnhandledAtStop(HandlerExecutionTask queuedTessageInformation) => _implementation.Update(implementation => implementation.LeftUnhandledAtStop(queuedTessageInformation));

         void RecordedFailedTry(HandlerExecutionTask queuedTessageInformation, Exception exception) => _implementation.Update(implementation => implementation.RecordedFailedTry(queuedTessageInformation, exception));

         void StartedWaitingToBeTriedAgain() => _implementation.Update(implementation => implementation.StartedWaitingToBeTriedAgain());

         void StoppedWaitingToBeTriedAgain() => _implementation.Update(implementation => implementation.StoppedWaitingToBeTriedAgain());

         public void Dispose() => _stopping.Dispose();

         public class NonThreadsafeImplementation(ITessagesInFlightTracker globalStateTracker, EndpointId endpointId)
         {
            const int MaxConcurrentlyExecutingHandlers = 20;
            readonly ITessagesInFlightTracker _globalStateTracker = globalStateTracker;
            readonly EndpointId _endpointId = endpointId;

            readonly List<HandlerExecutionTask> _tessagesWaitingToExecute = [];
            bool _dispatchingStopped;
            int _waitingToBeTriedAgainCount;
            AdmissionOfTessagesSentToItself _admissionOfTessagesSentToItself = AdmissionOfTessagesSentToItself.NotYetOpen;
            bool _tessagesSentToItselfTakeAPlaceInTheLine = true;

            ///<summary>How many tessages the endpoint sends itself are being admitted: from before their senders' transactions<br/>
            /// write their rows to those transactions' ends.</summary>
            internal int AdmissionsOfTessagesSentToItselfInProgress { get; private set; }

            ///<summary>False once the endpoint has begun to stop: a tessage it sends itself then waits for its next start.</summary>
            internal bool TakesTessagesSentToItselfIntoTheLine => _tessagesSentToItselfTakeAPlaceInTheLine && !_dispatchingStopped;

            ///<summary>True when nothing more will be handled before stopping: nothing is executing and nothing waits to<br/>
            /// execute, or every executing tessage waits to be tried again. A tessage waiting to be tried again holds the line: no<br/>
            /// tessage is dispatched while another executes.</summary>
            internal bool IsDrained => ExecutingCount == 0 ? WaitingCount == 0 : ExecutingCount == _waitingToBeTriedAgainCount;
            internal int WaitingCount => _tessagesWaitingToExecute.Count;
            internal int ExecutingCount { get; private set; }

            internal bool TryGetDispatchableTessage([NotNullWhen(true)] out HandlerExecutionTask? dispatchable)
            {
               dispatchable = null!;
               if(_dispatchingStopped || ExecutingCount >= MaxConcurrentlyExecutingHandlers)
               {
                  return false;
               }

               dispatchable = FirstDispatchableTessage();

               if (dispatchable == null)
               {
                  return false;
               }

               Dispatching(dispatchable);
               return true;
            }

            ///<summary>The first tessage in the line, once no tessage executes, passing over every tessage the endpoint sent itself<br/>
            /// whose sender hasn't committed, and every tessage the endpoint sent itself behind such a one: the endpoint's stream to<br/>
            /// itself is handled in the order its senders committed. Other streams don't wait for it.</summary>
            HandlerExecutionTask? FirstDispatchableTessage()
            {
               if(ExecutingCount > 0) return null;

               var aTessageSentToItselfAwaitsItsSendersCommit = false;
               foreach(var waiting in _tessagesWaitingToExecute)
               {
                  if(waiting.AwaitsItsSendersCommit)
                  {
                     aTessageSentToItselfAwaitsItsSendersCommit = true;
                     continue;
                  }

                  if(waiting.IsSentByTheEndpointToItself && aTessageSentToItselfAwaitsItsSendersCommit) continue;

                  return waiting;
               }

               return null;
            }

            internal void EnqueueTessageTask(HandlerExecutionTask tessage) => _tessagesWaitingToExecute.Add(tessage);

            internal void StopDispatching() => _dispatchingStopped = true;

            internal void StopTakingTessagesSentToItselfIntoTheLine() => _tessagesSentToItselfTakeAPlaceInTheLine = false;

            internal void StartAdmittingTessagesSentToItself()
            {
               State.Assert(_admissionOfTessagesSentToItself == AdmissionOfTessagesSentToItself.NotYetOpen);
               _admissionOfTessagesSentToItself = AdmissionOfTessagesSentToItself.Open;
            }

            internal void BeginAdmittingATessageSentToItself()
            {
               State.Assert(_admissionOfTessagesSentToItself != AdmissionOfTessagesSentToItself.NotYetOpen,
                            () => "The endpoint sent a tommand to itself before it had started. It handles such a tommand in its own inbox, where what its previous process left unhandled must take its place in the line first, being older: send it once the endpoint has started.");
               State.Assert(_admissionOfTessagesSentToItself != AdmissionOfTessagesSentToItself.Closed,
                            () => "The endpoint sent a tommand to itself after its stop had ended. It handles such a tommand in its own inbox, which the endpoint's next process may already own: send it before stopping the endpoint.");
               AdmissionsOfTessagesSentToItselfInProgress++;
            }

            internal void StopAdmittingTessagesSentToItself() => _admissionOfTessagesSentToItself = AdmissionOfTessagesSentToItself.Closed;

            ///<summary>The sender of a tessage the endpoint sent itself committed: the tessage, if it took a place in the line, may<br/>
            /// be handled, and is in flight until it is.</summary>
            internal void AdmissionOfATessageSentToItselfCommitted(HandlerExecutionTask? placeInTheLine)
            {
               AdmissionsOfTessagesSentToItselfInProgress--;
               if(placeInTheLine == null) return;
               _globalStateTracker.AdmittedTessageSentToItself(placeInTheLine.TransportTessage, _endpointId);
               placeInTheLine.ItsSenderCommitted();
            }

            ///<summary>The sender of a tessage the endpoint sent itself didn't commit: the tessage's row rolled back with it, and<br/>
            /// its place in the line is withdrawn.</summary>
            internal void AdmissionOfATessageSentToItselfWithdrawn(HandlerExecutionTask? placeInTheLine)
            {
               AdmissionsOfTessagesSentToItselfInProgress--;
               if(placeInTheLine != null) _tessagesWaitingToExecute.Remove(placeInTheLine);
            }

            enum AdmissionOfTessagesSentToItself
            {
               NotYetOpen,
               Open,
               Closed
            }

            internal void RecordedFailedTry(HandlerExecutionTask executing, Exception exception) => _globalStateTracker.FailedTry(executing.TransportTessage, _endpointId, exception);

            internal void StartedWaitingToBeTriedAgain() => _waitingToBeTriedAgainCount++;

            internal void StoppedWaitingToBeTriedAgain() => _waitingToBeTriedAgainCount--;

            internal void Succeeded(HandlerExecutionTask queuedTessageInformation) => DoneWith(queuedTessageInformation, exception: null);

            internal void Failed(HandlerExecutionTask queuedTessageInformation, Exception exception) => DoneWith(queuedTessageInformation, exception);

            ///<summary>The tessage's execution ended without it being handled or marked failed: the tessages-in-flight tracker<br/>
            /// is not told it is done, since it isn't.</summary>
            internal void LeftUnhandledAtStop(HandlerExecutionTask queuedTessageInformation) => StopExecuting(queuedTessageInformation);

            void Dispatching(HandlerExecutionTask dispatchable)
            {
               this.Log().Debug($"Dispatching {dispatchable.TransportTessage.TessageTypeEnum} tessage {dispatchable.TessageId} (executing: {ExecutingCount + 1}, waiting: {_tessagesWaitingToExecute.Count})");
               ExecutingCount++;
               _tessagesWaitingToExecute.Remove(dispatchable);
            }

            void DoneWith(HandlerExecutionTask doneExecuting, Exception? exception)
            {
               this.Log().Debug($"Done with {doneExecuting.TransportTessage.TessageTypeEnum} tessage {doneExecuting.TessageId}{(exception != null ? " (FAILED)" : "")} (executing: {ExecutingCount - 1}, waiting: {_tessagesWaitingToExecute.Count})");
               StopExecuting(doneExecuting);
               _globalStateTracker.DoneWith(doneExecuting.TransportTessage, _endpointId, exception);
            }

            void StopExecuting(HandlerExecutionTask executing) => ExecutingCount--;
         }
      }
   }
}
