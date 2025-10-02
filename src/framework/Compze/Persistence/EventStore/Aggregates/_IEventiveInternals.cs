using System;
using Compze.Messaging.Events;

namespace Compze.Persistence.EventStore.Aggregates;

public interface IEventiveInternals<in TEvent, in TEventImplementation>
    where TEventImplementation : AggregateEvent, TEvent
    where TEvent : class, IAggregateEvent
{
    private const string InternalUseOnly = "This breaks encapsulation of a teventive. It is for building infrastructure code. Such as generic reusable base classes for building teventives.";

    [Obsolete(InternalUseOnly)] void PublishInternal(TEventImplementation theEvent);
    [Obsolete(InternalUseOnly)] void ApplyEventInternal(TEvent @event);
    [Obsolete(InternalUseOnly)] IEventHandlerRegistrar<TEvent> RegisterEventAppliersInternal();
}
