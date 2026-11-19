using System.Transactions;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Engine.HandlerRegistration._private;
using Compze.Tessaging._internal.TessagesInFlight;
using Compze.Tessaging._private.SystemCE.ThreadingCE;
using Compze.Tessaging.TessageTypes;
using Compze.Threading;
using Compze.Threading.ResourceAccess;

namespace Compze.Tessaging.Engine._private;

sealed partial class TeventObservationDispatcher
{
   ///<summary>One observer's FIFO dispatch queue: observations queued for the observer, dispatched to it one at a time, in order,<br/>
   /// by a pump that runs only while there is work. The per-observer granularity is the isolation promise: a slow or failing<br/>
   /// observer delays only its own queue.</summary>
   ///<remarks>An observation is what one call to the observer gets: one tevent, or one unit of work's tevents as a list<br/>
   /// (<see cref="TeventObserverRegistration"/>). Each tevent in it counts as one observation in flight for the<br/>
   /// <see cref="ITessagesInFlightTracker"/>.</remarks>
   sealed class ObserverDispatchQueue
   {
      readonly TeventObserverRegistration _observer;
      readonly IScopeFactory _scopeFactory;
      readonly IBackgroundExceptionReporter _backgroundExceptionReporter;
      readonly ITessagesInFlightTracker _tessagesInFlightTracker;
      readonly ITaskRunner _taskRunner;
      readonly IAwaitableThreadShared<NonThreadSafeState> _state = IAwaitableThreadShared.New(new NonThreadSafeState());

      internal ObserverDispatchQueue(TeventObserverRegistration observer, IScopeFactory scopeFactory, IBackgroundExceptionReporter backgroundExceptionReporter, ITessagesInFlightTracker tessagesInFlightTracker, ITaskRunner taskRunner)
      {
         _observer = observer;
         _scopeFactory = scopeFactory;
         _backgroundExceptionReporter = backgroundExceptionReporter;
         _tessagesInFlightTracker = tessagesInFlightTracker;
         _taskRunner = taskRunner;
      }

      internal bool Observes(Type wrapperTeventType) => _observer.Observes(wrapperTeventType);

      ///<summary>Queues the observations <paramref name="committedTevents"/>, one unit of work's tevents, give this queue's observer.</summary>
      internal void EnqueueObservationsOf(IReadOnlyList<IPublisherTevent<ITevent>> committedTevents)
      {
         foreach(var observation in _observer.ObservationsOf(committedTevents)) Enqueue(observation);
      }

      void Enqueue(IReadOnlyList<IPublisherTevent<ITevent>> observation)
      {
         //Queued is reported before the enqueuing work item completes, so the tracker can never see a momentary at-rest between
         //a tessage finishing and its observation work becoming visible.
         ReportQueued(observation);
         var outcome = _state.Update(it =>
         {
            if(it.HasEnded) return EnqueueOutcome.DroppedSinceTheObserverHasEnded;
            it.PendingObservations.Enqueue(observation);
            if(it.Pumping) return EnqueueOutcome.Queued;
            it.Pumping = true;
            return EnqueueOutcome.QueuedAndThePumpMustStart;
         });

         switch(outcome)
         {
            case EnqueueOutcome.QueuedAndThePumpMustStart: StartPumpWithNoFlowingExecutionContext(); break;
            case EnqueueOutcome.DroppedSinceTheObserverHasEnded: ReportDispatched(observation); break;
         }
      }

      ///<summary>Ends this queue, for a dynamic observer that was disposed: nothing is queued from now on, and what is queued and<br/>
      /// not yet taken is dropped, no longer counting as in flight. A call already taken from the queue isn't waited for. Ending an<br/>
      /// ended queue does nothing more.</summary>
      internal void End()
      {
         var dropped = _state.Update(it =>
         {
            it.HasEnded = true;
            IReadOnlyList<IReadOnlyList<IPublisherTevent<ITevent>>> pending = [..it.PendingObservations];
            it.PendingObservations.Clear();
            return pending;
         });
         foreach(var observation in dropped) ReportDispatched(observation);
      }

      ///<summary>True if the queue had work to wait for; on return the queue is empty and its pump stopped (until new work arrives).</summary>
      internal async Task<bool> AwaitDrainedAsync(WaitTimeout timeout)
      {
         var hadWork = _state.Read(it => it.Pumping || it.PendingObservations.Count > 0);
         if(hadWork) await _state.AwaitAsync(it => it is { Pumping: false, PendingObservations.Count: 0 }, timeout: timeout).caf();
         return hadWork;
      }

      ///<summary>The pump must not inherit the enqueuing caller's execution context: observation is isolated from the caller in<br/>
      /// both directions, and a local publish enqueues from within the just-committed transaction's completion callback, whose<br/>
      /// ambient transaction would otherwise flow into the pump and enlist every observer's database work. Suppressing<br/>
      /// <see cref="ExecutionContext"/> flow starts the pump context-free; the pump asserts the resulting guarantee.</summary>
      void StartPumpWithNoFlowingExecutionContext()
      {
         if(ExecutionContext.IsFlowSuppressed())
         {
            _taskRunner.Run(PumpTaskName, PumpUntilEmptyAsync);
            return;
         }

         using(ExecutionContext.SuppressFlow())
            _taskRunner.Run(PumpTaskName, PumpUntilEmptyAsync);
      }

      const string PumpTaskName = "Tevent observation dispatch";

      ///<summary>Dispatches the queue's observations to the observer one at a time, in order: each is awaited before the next is<br/>
      /// taken, so the queue holds no thread while an async observer awaits, and stays FIFO.</summary>
      async Task PumpUntilEmptyAsync()
      {
         State.Assert(Transaction.Current == null,
                      () => $"The observation dispatch pump runs context-free — it is started with {nameof(ExecutionContext)} flow suppressed — yet an ambient transaction is present. An observer must never enlist in any caller's transaction.");
         while(true)
         {
            var nextObservation = _state.Update(IReadOnlyList<IPublisherTevent<ITevent>>? (it) =>
            {
               if(it.PendingObservations.Count > 0) return it.PendingObservations.Dequeue();
               it.Pumping = false;
               return null;
            });
            if(nextObservation == null) return;

            await DispatchToTheObserverAsync(nextObservation).caf();
            ReportDispatched(nextObservation);
         }
      }

      async Task DispatchToTheObserverAsync(IReadOnlyList<IPublisherTevent<ITevent>> observation)
      {
         try
         {
            //A fresh scope per invocation: an observer's resolutions belong to no caller's session, and on the context-free pump
            //there is no transaction for them to join.
            await _scopeFactory.ExecuteInIsolatedScopeAsync(scope => _observer.ObserveAsync(observation, scope)).caf();
         }
#pragma warning disable CA1031 //A throwing observer is reported, never retried - and must never stop its queue from dispatching the remaining observations.
         catch(Exception exception)
#pragma warning restore CA1031
         {
            _backgroundExceptionReporter.ReportException(exception);
         }
      }

      void ReportQueued(IReadOnlyList<IPublisherTevent<ITevent>> observation)
      {
         foreach(var tevent in observation) _tessagesInFlightTracker.TeventObservationQueued(tevent.GetType());
      }

      void ReportDispatched(IReadOnlyList<IPublisherTevent<ITevent>> observation)
      {
         foreach(var tevent in observation) _tessagesInFlightTracker.TeventObservationDispatched(tevent.GetType());
      }

      enum EnqueueOutcome
      {
         Queued,
         QueuedAndThePumpMustStart,
         DroppedSinceTheObserverHasEnded
      }

      class NonThreadSafeState
      {
         internal readonly Queue<IReadOnlyList<IPublisherTevent<ITevent>>> PendingObservations = new();
         internal bool Pumping;
         internal bool HasEnded;
      }
   }
}
