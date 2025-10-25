using System;
using Compze.Abstractions.Public;
using Compze.Abstractions.Tessaging.Teventive.EventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;

namespace Compze.Tessaging.Teventive;

public interface IEventiveInternals<in TEvent, in TEventImplementation>
    where TEventImplementation : AggregateTevent, TEvent
    where TEvent : class, IAggregateTevent
{
    [Obsolete(ObsoleteMessage.ForInternalUseOnly)] void PublishInternal(TEventImplementation theEvent);
    [Obsolete(ObsoleteMessage.ForInternalUseOnly)] void ApplyEventInternal(TEvent @event);
    [Obsolete(ObsoleteMessage.ForInternalUseOnly)] IEventHandlerRegistrar<TEvent> RegisterEventAppliersInternal();
}
