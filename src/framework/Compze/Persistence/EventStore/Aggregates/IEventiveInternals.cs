using Compze.Messaging.Events;

namespace Compze.Persistence.EventStore.Aggregates;

public interface IEventiveInternals<in TEvent, in TEventImplementation>
    where TEventImplementation : AggregateEvent, TEvent
    where TEvent : class, IAggregateEvent
{
    void Publish(TEventImplementation theEvent);
    void ApplyEvent(TEvent @event);
    IEventHandlerRegistrar<TEvent> RegisterEventAppliers();
}
