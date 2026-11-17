using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Contracts;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Engine.HandlerRegistration;
using Compze.Tessaging.Engine.HandlerRegistration._private;
using Compze.Tessaging._internal.TessagesInFlight;
using Compze.Tessaging._private.SystemCE.ThreadingCE;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.TessageTypes;
using Compze.Threading;
using Compze.Threading.ResourceAccess;

namespace Compze.Tessaging.Engine._private;

///<summary>The engine's tevent observation dispatch — its only background machinery: observers observe committed facts only,<br/>
/// off-thread, in per-observer FIFO order. A dispatch site queues committed tevents through <see cref="QueueCommittedTevents"/> —<br/>
/// the local publisher's unit of work when it commits, through <see cref="QueueAtTheCommitOf"/>, and an endpoint's arrival paths<br/>
/// as an already-committed fact arrives — and each matching observer's queue dispatches it in its own time: an observer's<br/>
/// failure never fails the publisher, and an observer's latency never blocks it, while an observing read model still sees the<br/>
/// tevents in order.</summary>
///<remarks>Each observer invocation runs in a fresh scope with no transaction: the dispatch pump is started with<br/>
/// <see cref="ExecutionContext"/> flow suppressed, so no caller's ambient transaction — least of all the just-committed one<br/>
/// whose completion callback queues a local publish — can leak into an observer. A throwing observer is reported through the<br/>
/// <see cref="IBackgroundExceptionReporter"/>, never retried, and never stops its queue. Queued-but-undispatched work is<br/>
/// reported to the <see cref="ITessagesInFlightTracker"/>, so the testing host's at-rest wait covers the observation queues.<br/>
/// Created with the engine, drained before the container tears down — no observation is discarded — because the drain runs<br/>
/// observers and observers need the container's scopes: an endpoint drains explicitly after its listening stops, and a plain<br/>
/// container's engine through <see cref="ILocalTessagingEngine.StopAsync"/>, which reaches <see cref="StopAsync"/>. The<br/>
/// container's own disposal of this singleton is only the backstop for a composition that stops neither.</remarks>
///<remarks>Two kinds of observer have queues here: those declared at composition, from the <see cref="TessageHandlerRoster"/>,<br/>
/// whose list never changes, and those created at runtime through <see cref="ObserveTevents"/>, whose list changes as they are<br/>
/// created and disposed. The runtime list is replaced whole under <see cref="_runtimeObserversMonitor"/>, and read without it, so<br/>
/// queueing a tevent never waits for an observer being created or disposed.</remarks>
sealed partial class TeventObservationDispatcher : IDisposable, IAsyncDisposable
{
   static readonly WaitTimeout DrainAtDisposalTimeout = WaitTimeout.Seconds(30);

   readonly IReadOnlyList<ObserverDispatchQueue> _declaredObserverQueues;
   readonly IReadOnlyList<Type> _teventTypesObservableAtRuntime;
   readonly IScopeFactory _scopeFactory;
   readonly IBackgroundExceptionReporter _backgroundExceptionReporter;
   readonly ITessagesInFlightTracker _tessagesInFlightTracker;
   readonly ITaskRunner _taskRunner;
   readonly IMonitor _runtimeObserversMonitor = IMonitor.New();
   volatile IReadOnlyList<ObserverDispatchQueue> _runtimeObserverQueues = [];
   readonly IThreadShared<Dictionary<UnitOfWorkId, List<IPublisherTevent<ITevent>>>> _teventsOfUncommittedUnitsOfWork = IThreadShared.New(new Dictionary<UnitOfWorkId, List<IPublisherTevent<ITevent>>>());
   bool _hasStopped;

   internal TeventObservationDispatcher(TessageHandlerRoster roster, IScopeFactory scopeFactory, IBackgroundExceptionReporter backgroundExceptionReporter, ITessagesInFlightTracker tessagesInFlightTracker, ITaskRunner taskRunner)
   {
      _scopeFactory = scopeFactory;
      _backgroundExceptionReporter = backgroundExceptionReporter;
      _tessagesInFlightTracker = tessagesInFlightTracker;
      _taskRunner = taskRunner;
      _declaredObserverQueues = [..roster.TeventObserverRegistrations.Select(NewQueueFor)];
      _teventTypesObservableAtRuntime = roster.TeventTypesObservableAtRuntime;
   }

   ///<summary>Collects <paramref name="wrappedTevent"/>, published in <paramref name="unitOfWork"/>, for its observers: the unit of<br/>
   /// work's tevents are queued together, in the order published, when it commits, so an observer of each unit of work gets them<br/>
   /// as one list. A unit of work that rolls back queues nothing.</summary>
   ///<remarks>Keyed by the unit of work's <see cref="UnitOfWorkId"/>, its transaction, so the tevents of every scope that publishes<br/>
   /// in the same transaction are one unit of work's.</remarks>
   public void QueueAtTheCommitOf(IUnitOfWorkResolver unitOfWork, IPublisherTevent<ITevent> wrappedTevent)
   {
      var teventsOfANewUnitOfWork = _teventsOfUncommittedUnitsOfWork.Locked(List<IPublisherTevent<ITevent>>? (teventsByUnitOfWork) =>
      {
         if(teventsByUnitOfWork.TryGetValue(unitOfWork.Id, out var tevents))
         {
            tevents.Add(wrappedTevent);
            return null;
         }

         teventsByUnitOfWork.Add(unitOfWork.Id, tevents = [wrappedTevent]);
         return tevents;
      });

      if(teventsOfANewUnitOfWork == null) return;
      //The list is captured, not looked up, so the order in which the two completion callbacks run doesn't matter.
      unitOfWork.OnCommittedSuccessfully(() => QueueCommittedTevents(teventsOfANewUnitOfWork));
      unitOfWork.OnCompleted(() => _teventsOfUncommittedUnitsOfWork.Locked(it => it.Remove(unitOfWork.Id)));
   }

   ///<summary>Queues <paramref name="wrappedTevent"/> — a committed fact that arrived alone, with no unit of work known to this<br/>
   /// process — for every observer whose subscription matches it. Returns immediately: dispatch is off-thread, per-observer FIFO.</summary>
   public void QueueForObservers(IPublisherTevent<ITevent> wrappedTevent) => QueueCommittedTevents([wrappedTevent]);

   ///<summary>Queues <paramref name="committedTevents"/>, the tevents of one committed unit of work in the order published, for<br/>
   /// every observer whose subscription matches any: each matching tevent for an observer of each tevent, and the matching ones as<br/>
   /// one list for an observer of each unit of work. Returns immediately: dispatch is off-thread, per-observer FIFO.</summary>
   void QueueCommittedTevents(IReadOnlyList<IPublisherTevent<ITevent>> committedTevents)
   {
      foreach(var queue in _declaredObserverQueues) queue.EnqueueObservationsOf(committedTevents);
      foreach(var queue in _runtimeObserverQueues) queue.EnqueueObservationsOf(committedTevents);
   }

   ///<summary>Whether any observer's subscription matches <paramref name="wrapperTeventType"/> — the deserialization-frugal<br/>
   /// question an arrival site asks from the envelope's type alone, so an arriving tessage nothing observes is never<br/>
   /// deserialized for observation's sake.</summary>
   public bool AnyTeventObserversFor(Type wrapperTeventType) =>
      _declaredObserverQueues.Any(it => it.Observes(wrapperTeventType)) || _runtimeObserverQueues.Any(it => it.Observes(wrapperTeventType));

   ///<summary>Creates the observers <paramref name="observe"/> declares, observing from now on, until the returned object is<br/>
   /// disposed: <see cref="ITeventObservation.ObserveTevents"/>.</summary>
   public IDisposable ObserveTevents(Action<TeventObservationRegistrar> observe)
   {
      State.Assert(!_hasStopped, () => $"The LocalTessagingEngine has stopped ({nameof(ILocalTessagingEngine)}.{nameof(ILocalTessagingEngine.StopAsync)}), so it publishes no more tevents: an observer created now would observe nothing. Create observers before stopping the engine.");
      List<TeventObserverRegistration> observers = [];
      var registrar = new TeventObservationRegistrar(observers.Add, declareObservableAtRuntime: null);
      observe(registrar);
      registrar.EndCallback();
      observers.ForEach(AssertDeclaredObservableAtRuntime);

      IReadOnlyList<ObserverDispatchQueue> queues = [..observers.Select(NewQueueFor)];
      _runtimeObserversMonitor.Locked(() => _runtimeObserverQueues = [.._runtimeObserverQueues, ..queues]);
      return new RuntimeObservation(this, queues);
   }

   ///<summary>Asserts that <paramref name="observer"/>, created at runtime, observes only tevents declared observable at runtime with<br/>
   /// <see cref="TeventObservationRegistrar.AtRuntime{TTevent}"/>: in an endpoint, only those are sent to it by its peers.</summary>
   void AssertDeclaredObservableAtRuntime(TeventObserverRegistration observer) =>
      State.Assert(_teventTypesObservableAtRuntime.Any(observer.ObservesOnlyTeventsCompatibleWith),
                   () => $"{observer.SubscribedTeventType.FullName} isn't declared observable at runtime, so an observer of it created at runtime could miss the tevents that arrive from other endpoints. Declare it, or a tevent type it is compatible with, where the engine is composed: ObserveTevents(observe => observe.{nameof(TeventObservationRegistrar.AtRuntime)}<{observer.SubscribedTeventType.Name}>()).");

   ///<summary>Ends the runtime observers of <paramref name="queues"/>: no tevent is queued for them any more, and what was queued and<br/>
   /// not yet taken is dropped. Doesn't wait for a call already running.</summary>
   void EndRuntimeObservation(IReadOnlyList<ObserverDispatchQueue> queues)
   {
      _runtimeObserversMonitor.Locked(() => _runtimeObserverQueues = [.._runtimeObserverQueues.Except(queues)]);
      foreach(var queue in queues) queue.End();
   }

   ObserverDispatchQueue NewQueueFor(TeventObserverRegistration observer) =>
      new(observer, _scopeFactory, _backgroundExceptionReporter, _tessagesInFlightTracker, _taskRunner);

   ///<summary>Drains every observer queue — no observation is discarded at disposal — holding no thread while it waits. An<br/>
   /// observer can itself publish (through an Independent* publisher), queueing further observation work on another queue, so<br/>
   /// draining runs in passes until one full pass finds every queue already empty. A queue that will not drain — a hanging<br/>
   /// observer — fails the drain loud on <see cref="DrainAtDisposalTimeout"/>.</summary>
   public async Task DrainAsync()
   {
      bool anyQueueHadWork;
      do
      {
         anyQueueHadWork = false;
         foreach(var queue in (IEnumerable<ObserverDispatchQueue>)[.._declaredObserverQueues, .._runtimeObserverQueues])
            anyQueueHadWork |= await queue.AwaitDrainedAsync(DrainAtDisposalTimeout).caf();
      } while(anyQueueHadWork);
   }

   ///<summary>Whether <see cref="StopAsync"/> has completed: the queues are drained and the engine publishes no more tevents.</summary>
   public bool HasStopped => _hasStopped;

   ///<summary>The stop of a plain container's engine (<see cref="ILocalTessagingEngine.StopAsync"/>): drains every queue, then<br/>
   /// marks the dispatch stopped, so a publisher refuses the tevents no observer would observe any more.</summary>
   public async Task StopAsync()
   {
      await DrainAsync().caf();
      _hasStopped = true;
   }

   public async ValueTask DisposeAsync() => await DrainAsync().caf();

   ///<summary>The backstop for a container disposed synchronously: an endpoint has drained already, so this waits only in a<br/>
   /// composition without the endpoint's lifecycle.</summary>
   public void Dispose() => DrainAsync().WaitUnwrappingException();
}
