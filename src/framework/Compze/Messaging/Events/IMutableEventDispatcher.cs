namespace Compze.Messaging.Events;

public interface IMutableEventDispatcher<in TEvent> : IEventDispatcher<TEvent>
   where TEvent : class, IEvent
{
   IEventHandlerRegistrar<TEvent> Register();
}