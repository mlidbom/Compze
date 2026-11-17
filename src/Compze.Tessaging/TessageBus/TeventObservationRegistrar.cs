using Compze.Contracts;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Engine;
using Compze.Tessaging.Engine.HandlerRegistration._private;
using Compze.Tessaging.Engine._private;
using Compze.Tessaging.TessageTypes;
using Compze.Tessaging.Validation._internal;

namespace Compze.Tessaging.TessageBus;

///<summary>Registers tevent observers — observation is the deliberately transaction-ignoring watch surface, declared under<br/>
/// its own verb so the distinct semantics are visible at the declaration site: an observer watches, never participates.<br/>
/// Handed to the callback of <see cref="LocalTessagingEngineBuilder.ObserveTevents"/> at composition, or of<br/>
/// <see cref="ITeventObservation.ObserveTevents"/> at runtime, and existing only inside it, exactly like<br/>
/// <see cref="TessageBusHandlerRegistrar"/>.</summary>
///<remarks>An observer observes committed facts only — a tevent published within an execution is queued for its observers at<br/>
/// commit, and an arriving tevent was committed by its publisher — and runs off-thread in per-observer FIFO order (the<br/>
/// engine's <see cref="TeventObservationDispatcher"/>). It receives a plain <see cref="IScopeResolver"/>, never a unit of<br/>
/// work: its invocation is a fresh scope with no transaction. An observer is synchronous or async: while an async observer<br/>
/// awaits, its queue holds no thread, and takes its next tevent only once the observer's task completes. A throwing observer is<br/>
/// reported through the background-exception reporter, never retried.</remarks>
public sealed class TeventObservationRegistrar : ITeventObservationRegistrar
{
   readonly Action<TeventObserverRegistration> _addObserver;
   readonly Action<Type>? _declareObservableAtRuntime;
   bool _callbackHasEnded;

   ///<summary>A registrar that adds each observer through <paramref name="addObserver"/>, and declares what may be observed at<br/>
   /// runtime through <paramref name="declareObservableAtRuntime"/>: at composition. Null for an observation created at runtime,<br/>
   /// where nothing more can be declared observable.</summary>
   internal TeventObservationRegistrar(Action<TeventObserverRegistration> addObserver, Action<Type>? declareObservableAtRuntime)
   {
      _addObserver = addObserver;
      _declareObservableAtRuntime = declareObservableAtRuntime;
   }

   ///<summary>Registers an observer for tevents compatible with <typeparamref name="TTevent"/>.</summary>
   public TeventObservationRegistrar ForTevent<TTevent>(Func<TTevent, IScopeResolver, Task> observer) where TTevent : ITevent =>
      Add(TeventObserverRegistration.ForEachTevent(observer));

   //One dispatch path: a synchronous observer is an async one whose task has completed by the time it returns.
   public TeventObservationRegistrar ForTevent<TTevent>(Action<TTevent, IScopeResolver> observer) where TTevent : ITevent =>
      ForTevent<TTevent>((tevent, scope) =>
      {
         observer(tevent, scope);
         return Task.CompletedTask;
      });

   ///<summary>Registers an observer of each committed unit of work's tevents compatible with <typeparamref name="TTevent"/>, as one<br/>
   /// list in the order they were published: for a reaction that is costly for each call, such as a view refreshing, or a read<br/>
   /// model writing to a database. It is called once for each committed unit of work that published any such tevent, never with<br/>
   /// an empty list.</summary>
   ///<remarks>A tevent with no unit of work known to this process comes as a list of its own: one published through<br/>
   /// <see cref="IIndependentTeventPublisher"/>, which is its own unit of work, and one that arrives from another endpoint, since<br/>
   /// nothing on it marks its sender's unit of work.<br/>
   /// review-naming: ForEachUnitOfWork is a placeholder name, for the naming pass (jastudio-rep-opus, 2026-11-17).</remarks>
   public TeventObservationRegistrar ForEachUnitOfWork<TTevent>(Func<IReadOnlyList<TTevent>, IScopeResolver, Task> observer) where TTevent : ITevent =>
      Add(TeventObserverRegistration.ForEachUnitOfWork(observer));

   //One dispatch path, as for ForTevent.
   public TeventObservationRegistrar ForEachUnitOfWork<TTevent>(Action<IReadOnlyList<TTevent>, IScopeResolver> observer) where TTevent : ITevent =>
      ForEachUnitOfWork<TTevent>((tevents, scope) =>
      {
         observer(tevents, scope);
         return Task.CompletedTask;
      });

   ///<summary>Declares, at composition, that tevents compatible with <typeparamref name="TTevent"/> may be observed by observers<br/>
   /// created at runtime through <see cref="ITeventObservation"/>, subtypes included. Every runtime observer's types must be<br/>
   /// covered by such a declaration.</summary>
   ///<remarks>An endpoint subscribes with its peers to the declared types from the start, as it does for an observer declared at<br/>
   /// composition. So a runtime observer is in effect the moment <see cref="ITeventObservation.ObserveTevents"/> returns, with no<br/>
   /// round trip: one created before a trigger can't miss a tevent published right after it. A tevent of a declared type that<br/>
   /// arrives while no runtime observer observes it is dropped. The declaration is required in every composition, a plain<br/>
   /// container's too, so that the same registrations run unchanged under any composition, and a runtime observer never silently<br/>
   /// misses the remote tevents of a type nobody declared.<br/>
   /// review-naming: AtRuntime is a placeholder name, for the naming pass (jastudio-rep-opus, 2026-11-17).</remarks>
   public TeventObservationRegistrar AtRuntime<TTevent>() where TTevent : ITevent
   {
      AssertUsedOnlyInsideItsCallback();
      var declareObservableAtRuntime = _declareObservableAtRuntime;
      State.Assert(declareObservableAtRuntime != null,
                   () => $"{nameof(AtRuntime)} declares at composition which tevents may be observed at runtime, and means nothing in an observation created at runtime. Declare {typeof(TTevent).FullName} in the composition's ObserveTevents, and observe it here with {nameof(ForTevent)} or {nameof(ForEachUnitOfWork)}.");
      TessageInspector.AssertValidForSubscription<TTevent>();
      declareObservableAtRuntime(typeof(TTevent));
      return this;
   }

   //The minimal registrar an endpoint-declaration's ObserveTevents override receives - this registrar's one facet.
   ITeventObservationRegistrar ITeventObservationRegistrar.ForTevent<TTevent>(Func<TTevent, IScopeResolver, Task> observer) => ForTevent(observer);

   ITeventObservationRegistrar ITeventObservationRegistrar.ForTevent<TTevent>(Action<TTevent, IScopeResolver> observer) => ForTevent(observer);

   ITeventObservationRegistrar ITeventObservationRegistrar.ForEachUnitOfWork<TTevent>(Func<IReadOnlyList<TTevent>, IScopeResolver, Task> observer) => ForEachUnitOfWork(observer);

   ITeventObservationRegistrar ITeventObservationRegistrar.ForEachUnitOfWork<TTevent>(Action<IReadOnlyList<TTevent>, IScopeResolver> observer) => ForEachUnitOfWork(observer);

   ITeventObservationRegistrar ITeventObservationRegistrar.AtRuntime<TTevent>() => AtRuntime<TTevent>();

   internal void EndCallback() => _callbackHasEnded = true;

   TeventObservationRegistrar Add(TeventObserverRegistration observer)
   {
      AssertUsedOnlyInsideItsCallback();
      _addObserver(observer);
      return this;
   }

   void AssertUsedOnlyInsideItsCallback() =>
      State.Assert(!_callbackHasEnded,
                   () => $"This {nameof(TeventObservationRegistrar)}'s declaration callback has ended, and the registrar exists only inside it: the callback's end is the registration's end, so nothing can hold a registrar and add observers later. Declare every observer inside the ObserveTevents callback.");
}
