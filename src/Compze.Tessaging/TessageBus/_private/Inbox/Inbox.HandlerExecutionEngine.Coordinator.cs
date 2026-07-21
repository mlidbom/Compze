using System.Diagnostics.CodeAnalysis;
using Compze.Contracts;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Engine._private;
using Compze.Tessaging._internal.TessagesInFlight;
using Compze.Tessaging._private.SystemCE.ThreadingCE;
using Compze.Tessaging._internal.Transport;
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
      partial class Coordinator(ITessagesInFlightTracker globalStateTracker, ITaskRunner taskRunner, ITessageStorage tessageStorage, IScopeFactory scopeFactory, TessageHandlerExecutor executor, EndpointId endpointId)
      {
         readonly ITaskRunner _taskRunner = taskRunner;
         readonly ITessageStorage _tessageStorage = tessageStorage;
         readonly IScopeFactory _scopeFactory = scopeFactory;
         readonly TessageHandlerExecutor _executor = executor;
         readonly IAwaitableThreadShared<NonThreadsafeImplementation> _implementation = IAwaitableThreadShared.New(new NonThreadsafeImplementation(globalStateTracker, endpointId));

         internal HandlerExecutionTask AwaitExecutableHandlerExecutionTask(IReadOnlyList<ITessageDispatchingRule> dispatchingRules, CancellationToken cancellationToken)
         {
            HandlerExecutionTask? handlerExecutionTask = null;
            _implementation.Await(implementation => implementation.TryGetDispatchableTessage(dispatchingRules, out handlerExecutionTask), cancellationToken);
            return handlerExecutionTask._assert().NotNull();
         }

         ///<summary>The shutdown drain: waits until the inbox is quiescent — every received tessage handled, nothing waiting or<br/>
         /// executing — so the endpoint tears down with empty queues. Best-effort: if the inbox has not gone idle within<br/>
         /// <paramref name="patience"/> (a handler is likely hung) it logs loudly and returns, leaving teardown to proceed rather<br/>
         /// than hang. Called after the transport has stopped, so nothing new arrives and the queue only shrinks.</summary>
         internal void AwaitAllReceivedTessagesProcessed(WaitTimeout patience)
         {
            if(_implementation.TryAwait(implementation => implementation.IsIdle, timeout: patience)) return;
            this.Log().Warning(_implementation.Read(implementation =>
               $"Shutdown drain: the inbox did not finish processing every received tessage within {patience} (waiting to execute: {implementation.WaitingCount}, executing: {implementation.ExecutingCount}). Proceeding with teardown - a handler is likely hung."));
         }

         internal Task<object?> EnqueueTessageTask(TransportTessage.InComing tessage) => _implementation.Update(implementation =>
         {
            this.Log().Debug($"Enqueueing {tessage.TessageTypeEnum} tessage {tessage.TessageId}");
            var inflightTessage = new HandlerExecutionTask(tessage, this, _taskRunner, _tessageStorage, _scopeFactory, _executor);
            implementation.EnqueueTessageTask(inflightTessage);
            return inflightTessage.Task;
         });

         void Succeeded(HandlerExecutionTask queuedTessageInformation) => _implementation.Update(implementation => implementation.Succeeded(queuedTessageInformation));

         void Failed(HandlerExecutionTask queuedTessageInformation, Exception exception) => _implementation.Update(implementation => implementation.Failed(queuedTessageInformation, exception));

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

            ///<summary>True when the inbox is quiescent: nothing is waiting to execute and no handler is executing.</summary>
            internal bool IsIdle => WaitingCount == 0 && ExecutingCount == 0;
            internal int WaitingCount => _tessagesWaitingToExecute.Count;
            internal int ExecutingCount { get; private set; }

            internal bool TryGetDispatchableTessage(IReadOnlyList<ITessageDispatchingRule> dispatchingRules, [NotNullWhen(true)] out HandlerExecutionTask? dispatchable)
            {
               dispatchable = null!;
               if(ExecutingCount >= MaxConcurrentlyExecutingHandlers)
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

            internal void Succeeded(HandlerExecutionTask queuedTessageInformation) => DoneDispatching(queuedTessageInformation);

            internal void Failed(HandlerExecutionTask queuedTessageInformation, Exception exception) => DoneDispatching(queuedTessageInformation, exception);

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

            //Refactor: Switching should not be necessary. See also inbox.
            void DoneDispatching(HandlerExecutionTask doneExecuting, Exception? exception = null)
            {
               this.Log().Debug($"Done with {doneExecuting.TransportTessage.TessageTypeEnum} tessage {doneExecuting.TessageId}{(exception != null ? " (FAILED)" : "")} (executing: {ExecutingCount - 1}, waiting: {_tessagesWaitingToExecute.Count})");
               ExecutingCount--;

               switch(doneExecuting.TransportTessage.TessageTypeEnum)
               {
                  case TransportTessageType.ExactlyOnceTevent:
                     _executingExactlyOnceTevents.Remove(doneExecuting.TransportTessage);
                     break;
                  case TransportTessageType.ExactlyOnceTommand:
                     _executingExactlyOnceTommands.Remove(doneExecuting.TransportTessage);
                     break;
                  case TransportTessageType.BestEffortTevent:
                  default:
                     throw new ArgumentOutOfRangeException();
               }

               _globalStateTracker.DoneWith(doneExecuting.TransportTessage, _endpointId, exception);
            }

            readonly List<TransportTessage.InComing> _executingExactlyOnceTommands = [];
            readonly List<TransportTessage.InComing> _executingExactlyOnceTevents = [];
         }
      }
   }
}
