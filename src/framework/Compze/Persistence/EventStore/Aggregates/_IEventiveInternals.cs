using System;
using Compze.Messaging.Events;

namespace Compze.Persistence.EventStore.Aggregates;

public interface IEventiveInternals<in TEvent, in TEventImplementation>
    where TEventImplementation : AggregateEvent, TEvent
    where TEvent : class, IAggregateEvent
{
    [Obsolete(InternalOnly.Message)] void PublishInternal(TEventImplementation theEvent);
    [Obsolete(InternalOnly.Message)] void ApplyEventInternal(TEvent @event);
    [Obsolete(InternalOnly.Message)] IEventHandlerRegistrar<TEvent> RegisterEventAppliersInternal();
}
