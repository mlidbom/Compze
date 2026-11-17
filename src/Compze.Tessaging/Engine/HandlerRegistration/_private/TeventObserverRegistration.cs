using Compze.Tessaging.Engine._private;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.TessageTypes;
using Compze.Tessaging.Validation._internal;

namespace Compze.Tessaging.Engine.HandlerRegistration._private;

///<summary>One tevent observer, as the engine holds it: declared at composition, in the <see cref="TessageHandlerRoster"/>, or<br/>
/// created at runtime through <see cref="ITeventObservation"/>. It pairs the wrapper routing key its subscription translates to<br/>
/// with the observing delegate. The engine's <see cref="TeventObservationDispatcher"/> builds one FIFO dispatch queue for each,<br/>
/// so each observer sees its observed tevents in order while never blocking, or being blocked by, another observer.</summary>
///<remarks>An observer observes either each tevent alone (<see cref="EachTevent"/>) or the tevents of each committed unit of work<br/>
/// as one list (<see cref="EachUnitOfWork"/>). What one call to the observer gets is an <i>observation</i>: a list of wrapped<br/>
/// tevents, which holds one tevent for an observer of each tevent.</remarks>
abstract partial class TeventObserverRegistration
{
   TeventObserverRegistration(Type subscribedTeventType)
   {
      SubscribedTeventType = subscribedTeventType;
      SubscribedWrapperType = PublisherTevent.WrapperTypeMatchingAllWrappingsOf(subscribedTeventType);
   }

   ///<summary>An observer called once for each tevent compatible with <typeparamref name="TTevent"/>. A subscription to a wrapper<br/>
   /// type receives the wrapper, and one to an inner tevent type the unwrapped tevent, as participation subscriptions do.</summary>
   internal static TeventObserverRegistration ForEachTevent<TTevent>(Func<TTevent, IScopeResolver, Task> observer) where TTevent : ITevent
   {
      TessageInspector.AssertValidForSubscription<TTevent>();
      return new EachTevent(typeof(TTevent), (wrappedTevent, scope) => observer(AsSubscribed<TTevent>(wrappedTevent), scope));
   }

   ///<summary>An observer called once for each committed unit of work that published tevents compatible with<br/>
   /// <typeparamref name="TTevent"/>, with those tevents as one list, in publish order.</summary>
   internal static TeventObserverRegistration ForEachUnitOfWork<TTevent>(Func<IReadOnlyList<TTevent>, IScopeResolver, Task> observer) where TTevent : ITevent
   {
      TessageInspector.AssertValidForSubscription<TTevent>();
      return new EachUnitOfWork(typeof(TTevent), (wrappedTevents, scope) => observer([..wrappedTevents.Select(AsSubscribed<TTevent>)], scope));
   }

   ///<summary>The tevent type the subscription names. Declared at composition, it joins the types the endpoint advertises to its<br/>
   /// peers, so that an observation-only subscription still pulls the tevent across the wire.</summary>
   internal Type SubscribedTeventType { get; }

   ///<summary>The wrapper type this observer's subscription matches: the routing key every dispatch site's wrapper type is tested<br/>
   /// against, exactly as participation subscriptions are keyed.</summary>
   Type SubscribedWrapperType { get; }

   ///<summary>Whether this observer's subscription matches <paramref name="wrapperTeventType"/>: tevent compatibility through the<br/>
   /// type hierarchy, the same rule participation routing uses.</summary>
   internal bool Observes(Type wrapperTeventType) => SubscribedWrapperType.IsAssignableFrom(wrapperTeventType);

   ///<summary>The observations that <paramref name="committedTevents"/>, the tevents one unit of work committed, in publish order,<br/>
   /// give this observer.</summary>
   internal abstract IEnumerable<IReadOnlyList<IPublisherTevent<ITevent>>> ObservationsOf(IReadOnlyList<IPublisherTevent<ITevent>> committedTevents);

   ///<summary>Calls the observer with <paramref name="observation"/>, in <paramref name="scope"/>, the fresh scope of the call.</summary>
   internal abstract Task ObserveAsync(IReadOnlyList<IPublisherTevent<ITevent>> observation, IScopeResolver scope);

   ///<summary>The tevents of <paramref name="committedTevents"/> this observer's subscription matches, in publish order.</summary>
   protected IEnumerable<IPublisherTevent<ITevent>> ObservedOf(IReadOnlyList<IPublisherTevent<ITevent>> committedTevents) =>
      committedTevents.Where(it => Observes(it.GetType()));

   static TTevent AsSubscribed<TTevent>(IPublisherTevent<ITevent> wrappedTevent) where TTevent : ITevent =>
      typeof(TTevent).Is<IPublisherTevent<ITevent>>() ? (TTevent)wrappedTevent : (TTevent)wrappedTevent.Tevent;
}
