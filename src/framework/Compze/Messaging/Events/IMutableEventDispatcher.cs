namespace Compze.Messaging.Events;

public interface IMutableEventDispatcher<in TEvent> : IEventDispatcher<TEvent>
   where TEvent : class, IEvent
{
   ///<summary>Registers handlers for the incoming events. All matching handlers will be called in the order they were registered.</summary>
   IEventHandlerRegistrar<TEvent> Register();

   static IMutableEventDispatcher<TEvent> New() => new CallMatchingHandlersInRegistrationOrderEventDispatcher<TEvent>();
}