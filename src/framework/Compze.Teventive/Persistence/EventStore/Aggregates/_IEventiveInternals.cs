using System;
using Compze.DDD.Abstractions;
using Compze.Tessaging.Teventive;

namespace Compze.Persistence.EventStore.Aggregates;

public interface IEventiveInternals<in TEvent, in TEventImplementation>
    where TEventImplementation : AggregateEvent, TEvent
    where TEvent : class, IAggregateEvent
{
    [Obsolete(ObsoleteMessage.ForInternalUseOnly)] void PublishInternal(TEventImplementation theEvent);
    [Obsolete(ObsoleteMessage.ForInternalUseOnly)] void ApplyEventInternal(TEvent @event);
    [Obsolete(ObsoleteMessage.ForInternalUseOnly)] IEventHandlerRegistrar<TEvent> RegisterEventAppliersInternal();
}
