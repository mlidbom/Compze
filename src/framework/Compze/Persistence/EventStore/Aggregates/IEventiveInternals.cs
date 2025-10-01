using Compze.Messaging.Events;

namespace Compze.Persistence.EventStore.Aggregates;

public interface IEventiveInternals<in TEventImplementation, in TEvent>
    where TEventImplementation : AggregateEvent, TEvent
    where TEvent : class, IAggregateEvent
{
    void Publish(TEventImplementation theEvent);
    IEventHandlerRegistrar<TEvent> RegisterEventAppliers();
}
