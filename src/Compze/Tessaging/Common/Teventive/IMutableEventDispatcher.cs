using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Tessaging.Abstractions;

namespace Compze.Tessaging.Common.Teventive;

public interface IMutableEventDispatcher<in TEvent> : IEventDispatcher<TEvent>
   where TEvent : class, IEvent
{
   ///<summary>Registers handlers for the incoming events. All matching handlers will be called in the order they were registered.</summary>
   IEventHandlerRegistrar<TEvent> Register();

   ///<summary>Returns true if this dispatcher has any handlers that would handle the given event.</summary>
   bool Handles(TEvent @event);

   static IMutableEventDispatcher<TEvent> New() => new CallMatchingHandlersInRegistrationOrderEventDispatcher<TEvent>();
}