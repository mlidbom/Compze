using Compze.Abstractions.Tessaging.Public;
using Compze.Contracts;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Engine;

///<summary>The declaration surface for tevent observation — the deliberately transaction-ignoring watch surface, declared under<br/>
/// its own verb (<see cref="LocalTessagingEngineBuilder.ObserveTevents"/>) so the distinct semantics are visible at the<br/>
/// declaration site: an observer watches, never participates. Handed to the callback and existing only inside it, exactly like<br/>
/// <see cref="TessageHandlerRegistrar"/>.</summary>
///<remarks>An observer observes committed facts only — a tevent published within an execution is queued for its observers at<br/>
/// commit, and an arriving tevent was committed by its publisher — and runs off-thread in per-observer FIFO order (the<br/>
/// engine's <see cref="TeventObservationDispatcher"/>). It receives a plain <see cref="IScopeResolver"/>, never a unit of<br/>
/// work: its invocation is a fresh scope with no transaction. A throwing observer is reported through the<br/>
/// background-exception reporter, never retried.</remarks>
public sealed class TeventObservationRegistrar
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

   internal void EndCallback() => _callbackHasEnded = true;

   void AssertUsedOnlyInsideItsCallback() =>
      State.Assert(!_callbackHasEnded,
                   () => $"This {nameof(TeventObservationRegistrar)}'s declaration callback has ended, and the registrar exists only inside it: the callback's end is the registration's end, so nothing can hold a registrar and mutate the engine later. Declare every observer inside the {nameof(LocalTessagingEngineBuilder.ObserveTevents)} callback.");
}
