using Compze.Tessaging.Engine.Internal;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.Engine.HandlerRegistration.Internal;

///<summary>One declared tevent observer, as the <see cref="TessageHandlerRoster"/> holds it: the wrapper routing key the<br/>
/// subscription translates to (see <see cref="TessageHandlerRegistrations.AddTeventObserver{TTevent}"/>) paired with the<br/>
/// observing delegate, which receives the wrapped tevent and unwraps it per the subscription's shape. The engine's<br/>
/// <see cref="TeventObservationDispatcher"/> builds one FIFO dispatch queue per registration, so each observer sees its<br/>
/// observed tevents in order while never blocking, or being blocked by, another observer.</summary>
sealed class TeventObserverRegistration
{
   internal TeventObserverRegistration(Type subscribedWrapperType, Action<ITevent, IScopeResolver> observe)
   {
      SubscribedWrapperType = subscribedWrapperType;
      Observe = observe;
   }

   ///<summary>The wrapper type this observer's subscription matches — the routing key every dispatch site's wrapper type is<br/>
   /// tested against, exactly as participation subscriptions are keyed.</summary>
   Type SubscribedWrapperType { get; }

   ///<summary>The observing delegate: receives the wrapped tevent and the fresh scope its invocation runs in.</summary>
   internal Action<ITevent, IScopeResolver> Observe { get; }

   ///<summary>Whether this observer's subscription matches <paramref name="wrapperTeventType"/> — tevent compatibility through<br/>
   /// the type hierarchy, the same rule participation routing uses.</summary>
   internal bool Observes(Type wrapperTeventType) => SubscribedWrapperType.IsAssignableFrom(wrapperTeventType);
}
