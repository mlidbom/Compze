using Compze.Contracts;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Engine;
using Compze.Tessaging.Engine.HandlerRegistration._private;
using Compze.Tessaging.Engine._private;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.TessageBus;

///<summary>The declaration surface for tevent observation — the deliberately transaction-ignoring watch surface, declared under<br/>
/// its own verb (<see cref="LocalTessagingEngineBuilder.ObserveTevents"/>) so the distinct semantics are visible at the<br/>
/// declaration site: an observer watches, never participates. Handed to the callback and existing only inside it, exactly like<br/>
/// <see cref="TessageBusHandlerRegistrar"/>.</summary>
///<remarks>An observer observes committed facts only — a tevent published within an execution is queued for its observers at<br/>
/// commit, and an arriving tevent was committed by its publisher — and runs off-thread in per-observer FIFO order (the<br/>
/// engine's <see cref="TeventObservationDispatcher"/>). It receives a plain <see cref="IScopeResolver"/>, never a unit of<br/>
/// work: its invocation is a fresh scope with no transaction. A throwing observer is reported through the<br/>
/// background-exception reporter, never retried.</remarks>
public sealed class TeventObservationRegistrar : ITeventObservationRegistrar
{
   readonly TessageHandlerRegistrations _registrations;
   bool _callbackHasEnded;

   internal TeventObservationRegistrar(TessageHandlerRegistrations registrations) => _registrations = registrations;

   ///<summary>Registers an observer for tevents compatible with <typeparamref name="TTevent"/>.</summary>
   public TeventObservationRegistrar ForTevent<TTevent>(Action<TTevent, IScopeResolver> observer) where TTevent : ITevent
   {
      AssertUsedOnlyInsideItsCallback();
      _registrations.AddTeventObserver(observer);
      return this;
   }

   //The minimal registrar an endpoint-declaration's ObserveTevents override receives - this registrar's one facet.
   ITeventObservationRegistrar ITeventObservationRegistrar.ForTevent<TTevent>(Action<TTevent, IScopeResolver> observer)
   {
      ForTevent(observer);
      return this;
   }

   internal void EndCallback() => _callbackHasEnded = true;

   void AssertUsedOnlyInsideItsCallback() =>
      State.Assert(!_callbackHasEnded,
                   () => $"This {nameof(TeventObservationRegistrar)}'s declaration callback has ended, and the registrar exists only inside it: the callback's end is the registration's end, so nothing can hold a registrar and mutate the engine later. Declare every observer inside the {nameof(LocalTessagingEngineBuilder.ObserveTevents)} callback.");
}
