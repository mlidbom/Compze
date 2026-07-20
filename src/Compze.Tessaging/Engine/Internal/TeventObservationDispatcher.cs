using System.Transactions;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Engine.HandlerRegistration;
using Compze.Tessaging.Engine.HandlerRegistration.Internal;
using Compze.Tessaging.Engine.HandlerRegistration.TeventObservation;
using Compze.Tessaging.TessageBus.Internal.BestEffortDelivery;
using Compze.Tessaging.TessageBus.Internal.Outbox;
using Compze.Tessaging.Internal.TessagesInFlight;
using Compze.Tessaging.Internal.SystemCE.ThreadingCE;
using Compze.Threading;
using Compze.Threading.ResourceAccess;

namespace Compze.Tessaging.Engine.Internal;

///<summary>The engine's tevent observation dispatch — its only background machinery: observers observe committed facts only,<br/>
/// off-thread, in per-observer FIFO order. A dispatch site queues a wrapped tevent through <see cref="QueueForObservers"/> —<br/>
/// the local publisher when its unit of work commits, an endpoint's arrival paths as an already-committed fact arrives — and<br/>
/// each matching observer's queue dispatches it in its own time: an observer's failure never fails the publisher, and an<br/>
/// observer's latency never blocks it, while an observing read model still sees the tevents in order.</summary>
///<remarks>Each observer invocation runs in a fresh scope with no transaction: the dispatch pump is started with<br/>
/// <see cref="ExecutionContext"/> flow suppressed, so no caller's ambient transaction — least of all the just-committed one<br/>
/// whose completion callback queues a local publish — can leak into an observer. A throwing observer is reported through the<br/>
/// <see cref="IBackgroundExceptionReporter"/>, never retried, and never stops its queue. Queued-but-undispatched work is<br/>
/// reported to the <see cref="ITessagesInFlightTracker"/>, so the testing host's at-rest wait covers the observation queues.<br/>
/// Created with the engine, drained at disposal — no observation is discarded: an endpoint drains explicitly after its<br/>
/// listening stops and before its container tears down, because the drain runs observers and observers need the container's<br/>
/// scopes; the container's own disposal of this singleton is only the backstop for compositions without such a lifecycle.</remarks>
sealed class TeventObservationDispatcher : IDisposable
{
   static readonly WaitTimeout DrainAtDisposalTimeout = WaitTimeout.Seconds(30);

   readonly IReadOnlyList<ObserverDispatchQueue> _observerQueues;

   internal TeventObservationDispatcher(TessageHandlerRoster roster, IScopeFactory scopeFactory, IBackgroundExceptionReporter backgroundExceptionReporter, ITessagesInFlightTracker tessagesInFlightTracker, ITaskRunner taskRunner)
      => _observerQueues = [..roster.TeventObserverRegistrations.Select(observer => new ObserverDispatchQueue(observer, scopeFactory, backgroundExceptionReporter, tessagesInFlightTracker, taskRunner))];

   ///<summary>Queues <paramref name="wrappedTevent"/> — a committed fact — for every observer whose subscription matches it.<br/>
   /// Returns immediately: dispatch is off-thread, per-observer FIFO.</summary>
   public void QueueForObservers(IPublisherTevent<ITevent> wrappedTevent)
   {
      foreach(var queue in _observerQueues)
      {
         if(queue.Observes(wrappedTevent.GetType()))
            queue.Enqueue(wrappedTevent);
      }
   }

   ///<summary>Whether any observer's subscription matches <paramref name="wrapperTeventType"/> — the deserialization-frugal<br/>
   /// question an arrival site asks from the envelope's type alone, so an arriving tessage nothing observes is never<br/>
   /// deserialized for observation's sake.</summary>
   public bool AnyTeventObserversFor(Type wrapperTeventType) => _observerQueues.Any(it => it.Observes(wrapperTeventType));

   ///<summary>Drains every observer queue — no observation is discarded at disposal. An observer can itself publish (through an<br/>
   /// independent door), queueing further observation work on another queue, so draining runs in passes until one full pass<br/>
   /// finds every queue already empty. A queue that will not drain — a hanging observer — fails the disposal loud on <see cref="DrainAtDisposalTimeout"/>.</summary>
   public void Dispose()
   {
      bool anyQueueHadWork;
      do
      {
         anyQueueHadWork = false;
         foreach(var queue in _observerQueues)
            anyQueueHadWork |= queue.AwaitDrained(DrainAtDisposalTimeout);
      } while(anyQueueHadWork);
   }

   ///<summary>One observer's FIFO dispatch queue: tevents queued for the observer, dispatched to it one at a time, in order, by<br/>
   /// a pump that runs only while there is work. The per-observer granularity is the isolation promise: a slow or failing<br/>
   /// observer delays only its own queue.</summary>
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

      internal void Enqueue(IPublisherTevent<ITevent> wrappedTevent)
      {
         //Queued is reported before the enqueuing work item completes, so the tracker can never see a momentary at-rest between
         //a tessage finishing and its observation work becoming visible.
         _tessagesInFlightTracker.TeventObservationQueued(wrappedTevent.GetType());
         var mustStartPump = _state.Update(it =>
         {
            it.PendingTevents.Enqueue(wrappedTevent);
            if(it.Pumping) return false;
            it.Pumping = true;
            return true;
         });
         if(mustStartPump) StartPumpWithNoFlowingExecutionContext();
      }

      ///<summary>True if the queue had work to wait for; on return the queue is empty and its pump stopped (until new work arrives).</summary>
      internal bool AwaitDrained(WaitTimeout timeout)
      {
         var hadWork = _state.Read(it => it.Pumping || it.PendingTevents.Count > 0);
         if(hadWork) _state.Await(it => it is { Pumping: false, PendingTevents.Count: 0 }, timeout: timeout);
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
            _taskRunner.Run(PumpTaskName, PumpUntilEmpty);
            return;
         }

         using(ExecutionContext.SuppressFlow())
            _taskRunner.Run(PumpTaskName, PumpUntilEmpty);
      }

      const string PumpTaskName = "Tevent observation dispatch";

      void PumpUntilEmpty()
      {
         State.Assert(Transaction.Current == null,
                      () => $"The observation dispatch pump runs context-free — it is started with {nameof(ExecutionContext)} flow suppressed — yet an ambient transaction is present. An observer must never enlist in any caller's transaction.");
         while(true)
         {
            var nextTevent = _state.Update(IPublisherTevent<ITevent>? (it) =>
            {
               if(it.PendingTevents.Count > 0) return it.PendingTevents.Dequeue();
               it.Pumping = false;
               return null;
            });
            if(nextTevent == null) return;

            DispatchToTheObserver(nextTevent);
            _tessagesInFlightTracker.TeventObservationDispatched(nextTevent.GetType());
         }
      }

      void DispatchToTheObserver(IPublisherTevent<ITevent> wrappedTevent)
      {
         try
         {
            //A fresh scope per invocation: an observer's resolutions belong to no caller's session, and on the context-free pump
            //there is no transaction for them to join.
            _scopeFactory.ExecuteInIsolatedScope(scope => _observer.Observe(wrappedTevent, scope));
         }
#pragma warning disable CA1031 //A throwing observer is reported, never retried - and must never stop its queue from dispatching the remaining observations.
         catch(Exception exception)
#pragma warning restore CA1031
         {
            _backgroundExceptionReporter.ReportException(exception);
         }
      }

      class NonThreadSafeState
      {
         internal readonly Queue<IPublisherTevent<ITevent>> PendingTevents = new();
         internal bool Pumping;
      }
   }
}
