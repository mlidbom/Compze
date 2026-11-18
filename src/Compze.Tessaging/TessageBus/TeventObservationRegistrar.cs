using Compze.Contracts;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Engine;
using Compze.Tessaging.Engine.HandlerRegistration._private;
using Compze.Tessaging.Engine._private;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.TessageBus;

///<summary>Registers tevent observers — observation is the deliberately transaction-ignoring watch surface, declared under<br/>
/// its own verb so the distinct semantics are visible at the declaration site: an observer watches, never participates.<br/>
/// Handed to the callback of <see cref="LocalTessagingEngineBuilder.ObserveTevents"/> at composition, for composed observers, or<br/>
/// of <see cref="ITeventObservation.ObserveTevents"/> while the engine runs, for dynamic observers, and existing only inside it,<br/>
/// exactly like<br/>
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
   bool _callbackHasEnded;

   ///<summary>A registrar that adds each observer through <paramref name="addObserver"/>: composed observers at composition, or<br/>
   /// dynamic observers while the engine runs.</summary>
   internal TeventObservationRegistrar(Action<TeventObserverRegistration> addObserver) => _addObserver = addObserver;

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

   //The minimal registrar an endpoint-declaration's ObserveTevents override receives - this registrar's one facet.
   ITeventObservationRegistrar ITeventObservationRegistrar.ForTevent<TTevent>(Func<TTevent, IScopeResolver, Task> observer) => ForTevent(observer);

   ITeventObservationRegistrar ITeventObservationRegistrar.ForTevent<TTevent>(Action<TTevent, IScopeResolver> observer) => ForTevent(observer);

   ITeventObservationRegistrar ITeventObservationRegistrar.ForEachUnitOfWork<TTevent>(Func<IReadOnlyList<TTevent>, IScopeResolver, Task> observer) => ForEachUnitOfWork(observer);

   ITeventObservationRegistrar ITeventObservationRegistrar.ForEachUnitOfWork<TTevent>(Action<IReadOnlyList<TTevent>, IScopeResolver> observer) => ForEachUnitOfWork(observer);

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
