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
using Compze.Tessaging.TessageBus._private.TessageHandling.Dispatching;
using Compze.Threading;
using Compze.Threading.ResourceAccess;
using Compze.Tessaging._private.Transport;

namespace Compze.Tessaging.TessageBus._private.Inbox;

partial class Inbox
{
   partial class HandlerExecutionEngine
   {
      //refactor: Consider moving all tessage type specific responsibilities into the tessage class or other class. Probably create more subtypes so that no type checking is required. See also inbox.
      partial class Coordinator(ITessagesInFlightTracker globalStateTracker, ITaskRunner taskRunner, ITessageStorage tessageStorage, IScopeFactory scopeFactory, TessageHandlerExecutor executor, RetryPolicy retryPolicy, EndpointId endpointId) : IDisposable
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
         internal HandlerExecutionTask AwaitExecutableHandlerExecutionTask(IReadOnlyList<ITessageDispatchingRule> dispatchingRules)
         {
            HandlerExecutionTask? handlerExecutionTask = null;
            _implementation.Await(implementation => implementation.TryGetDispatchableTessage(dispatchingRules, out handlerExecutionTask), _stopping.Token);
            return handlerExecutionTask._assert().NotNull();
         }

         ///<summary>The shutdown drain, then the stop. Waits until nothing more will be handled before stopping<br/>
         /// (<see cref="NonThreadsafeImplementation.IsDrained"/>), then stops handling (<see cref="StopAsync"/>), and waits for a<br/>
         /// try that was still running to end, so that this endpoint's process handles nothing after it returns. A tessage that<br/>
         /// waits to be tried again, and those behind it, are left unhandled, for the recovery scan at the endpoint's next start.<br/>
         /// Best-effort: if a wait lasts longer than <paramref name="patience"/> (a handler is likely hung) it logs loudly and<br/>
         /// returns, leaving teardown to proceed rather than hang. Called after the transport has stopped, so nothing new<br/>
         /// arrives.</summary>
         internal async Task DrainAndStopAsync(WaitTimeout patience)
         {
            var drained = await _implementation.TryAwaitAsync(implementation => implementation.IsDrained, timeout: patience).caf();
            if(!drained)
               this.Log().Warning(_implementation.Read(implementation =>
                  $"Shutdown drain: the inbox did not finish processing every received tessage within {patience} (waiting to execute: {implementation.WaitingCount}, executing: {implementation.ExecutingCount}). Proceeding with teardown - a handler is likely hung."));

            await StopAsync().caf();

            if(drained && !await _implementation.TryAwaitAsync(implementation => implementation.ExecutingCount == 0, timeout: patience).caf())
               this.Log().Warning($"Shutdown drain: a try of a tessage's handling was still running {patience} after handling stopped. Proceeding with teardown - a handler is likely hung.");
         }

         ///<summary>Stops handling: nothing more is dispatched, and every wait of a tessage to be tried again ends, leaving the<br/>
         /// tessage unhandled for the recovery scan at the endpoint's next start. Calling it again does nothing more.</summary>
         internal async Task StopAsync()
         {
            _implementation.Update(implementation => implementation.StopDispatching());
            await _stopping.CancelAsync().caf();
         }

         internal void EnqueueTessageTask(TransportTessage.InComing tessage, TimeSpan failingTime) => _implementation.Update(implementation =>
         {
            this.Log().Debug($"Enqueueing {tessage.TessageTypeEnum} tessage {tessage.TessageId}");
            implementation.EnqueueTessageTask(new HandlerExecutionTask(tessage, failingTime, this, _taskRunner, _tessageStorage, _scopeFactory, _executor, _retryPolicy, _stopping.Token));
         });

         void Succeeded(HandlerExecutionTask queuedTessageInformation) => _implementation.Update(implementation => implementation.Succeeded(queuedTessageInformation));

         void Failed(HandlerExecutionTask queuedTessageInformation, Exception exception) => _implementation.Update(implementation => implementation.Failed(queuedTessageInformation, exception));

         void LeftUnhandledAtStop(HandlerExecutionTask queuedTessageInformation) => _implementation.Update(implementation => implementation.LeftUnhandledAtStop(queuedTessageInformation));

         void RecordedFailedTry(HandlerExecutionTask queuedTessageInformation, Exception exception) => _implementation.Update(implementation => implementation.RecordedFailedTry(queuedTessageInformation, exception));

         void StartedWaitingToBeTriedAgain() => _implementation.Update(implementation => implementation.StartedWaitingToBeTriedAgain());

         void StoppedWaitingToBeTriedAgain() => _implementation.Update(implementation => implementation.StoppedWaitingToBeTriedAgain());

         public void Dispose() => _stopping.Dispose();

         public class NonThreadsafeImplementation(ITessagesInFlightTracker globalStateTracker, EndpointId endpointId) : IExecutingTessagesSnapshot
         {
            const int MaxConcurrentlyExecutingHandlers = 20;
            readonly ITessagesInFlightTracker _globalStateTracker = globalStateTracker;
            readonly EndpointId _endpointId = endpointId;


            //performance: Split waiting tessages into prioritized categories: Exactly once tevent/tommand, At most once tevent/tommand,  NonTransactional tuery
            //don't postpone checking if mutations are allowed to run because we have a ton of tueries queued up. Also the tueries are likely not allowed to run due to the tommands and tevents!
            //performance: Use static type caching trick to ensure that we know which rules need to be applied to which tessages. Don't check rules that don't apply. (Double dispatching might be required.)
            public IReadOnlyList<TransportTessage.InComing> ExactlyOnceTommands => _executingExactlyOnceTommands;
            public IReadOnlyList<TransportTessage.InComing> ExactlyOnceTevents => _executingExactlyOnceTevents;

            readonly List<HandlerExecutionTask> _tessagesWaitingToExecute = [];
            bool _dispatchingStopped;
            int _waitingToBeTriedAgainCount;

            ///<summary>True when nothing more will be handled before stopping: nothing is executing and nothing waits to<br/>
            /// execute, or every executing tessage waits to be tried again. A tessage waiting to be tried again holds the line: the<br/>
            /// dispatching rules dispatch no exactly-once tessage while another executes.</summary>
            internal bool IsDrained => ExecutingCount == 0 ? WaitingCount == 0 : ExecutingCount == _waitingToBeTriedAgainCount;
            internal int WaitingCount => _tessagesWaitingToExecute.Count;
            internal int ExecutingCount { get; private set; }

            internal bool TryGetDispatchableTessage(IReadOnlyList<ITessageDispatchingRule> dispatchingRules, [NotNullWhen(true)] out HandlerExecutionTask? dispatchable)
            {
               dispatchable = null!;
               if(_dispatchingStopped || ExecutingCount >= MaxConcurrentlyExecutingHandlers)
               {
                  return false;
               }

               dispatchable = _tessagesWaitingToExecute
                 .FirstOrDefault(queuedTask => dispatchingRules.All(rule => rule.CanBeDispatched(this, queuedTask.TransportTessage)));

               if (dispatchable == null)
               {
                  return false;
               }

               Dispatching(dispatchable);
               return true;
            }

            internal void EnqueueTessageTask(HandlerExecutionTask tessage) => _tessagesWaitingToExecute.Add(tessage);

            internal void StopDispatching() => _dispatchingStopped = true;

            internal void RecordedFailedTry(HandlerExecutionTask executing, Exception exception) => _globalStateTracker.FailedTry(executing.TransportTessage, _endpointId, exception);

            internal void StartedWaitingToBeTriedAgain() => _waitingToBeTriedAgainCount++;

            internal void StoppedWaitingToBeTriedAgain() => _waitingToBeTriedAgainCount--;

            internal void Succeeded(HandlerExecutionTask queuedTessageInformation) => DoneWith(queuedTessageInformation, exception: null);

            internal void Failed(HandlerExecutionTask queuedTessageInformation, Exception exception) => DoneWith(queuedTessageInformation, exception);

            ///<summary>The tessage's execution ended without it being handled or marked failed: the tessages-in-flight tracker<br/>
            /// is not told it is done, since it isn't.</summary>
            internal void LeftUnhandledAtStop(HandlerExecutionTask queuedTessageInformation) => StopExecuting(queuedTessageInformation);

            //Refactor: Switching should not be necessary. See also inbox.
            void Dispatching(HandlerExecutionTask dispatchable)
            {
               this.Log().Debug($"Dispatching {dispatchable.TransportTessage.TessageTypeEnum} tessage {dispatchable.TessageId} (executing: {ExecutingCount + 1}, waiting: {_tessagesWaitingToExecute.Count})");
               ExecutingCount++;

               switch(dispatchable.TransportTessage.TessageTypeEnum)
               {
                  case TransportTessageType.ExactlyOnceTevent:
                     _executingExactlyOnceTevents.Add(dispatchable.TransportTessage);
                     break;
                  case TransportTessageType.ExactlyOnceTommand:
                     _executingExactlyOnceTommands.Add(dispatchable.TransportTessage);
                     break;
                  case TransportTessageType.BestEffortTevent:
                  default:
                     throw new ArgumentOutOfRangeException();
               }

               _tessagesWaitingToExecute.Remove(dispatchable);
            }

            void DoneWith(HandlerExecutionTask doneExecuting, Exception? exception)
            {
               this.Log().Debug($"Done with {doneExecuting.TransportTessage.TessageTypeEnum} tessage {doneExecuting.TessageId}{(exception != null ? " (FAILED)" : "")} (executing: {ExecutingCount - 1}, waiting: {_tessagesWaitingToExecute.Count})");
               StopExecuting(doneExecuting);
               _globalStateTracker.DoneWith(doneExecuting.TransportTessage, _endpointId, exception);
            }

            //Refactor: Switching should not be necessary. See also inbox.
            void StopExecuting(HandlerExecutionTask executing)
            {
               ExecutingCount--;

               switch(executing.TransportTessage.TessageTypeEnum)
               {
                  case TransportTessageType.ExactlyOnceTevent:
                     _executingExactlyOnceTevents.Remove(executing.TransportTessage);
                     break;
                  case TransportTessageType.ExactlyOnceTommand:
                     _executingExactlyOnceTommands.Remove(executing.TransportTessage);
                     break;
                  case TransportTessageType.BestEffortTevent:
                  default:
                     throw new ArgumentOutOfRangeException();
               }
            }

            readonly List<TransportTessage.InComing> _executingExactlyOnceTommands = [];
            readonly List<TransportTessage.InComing> _executingExactlyOnceTevents = [];
         }
      }
   }
}
