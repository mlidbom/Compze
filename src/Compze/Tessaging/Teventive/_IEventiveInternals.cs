using System;
using Compze.Abstractions.Public;
using Compze.Abstractions.Tessaging.Teventive.Eventstore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;

namespace Compze.Tessaging.Teventive;

public interface IEventiveInternals<in TEvent, in TEventImplementation>
    where TEventImplementation : AggregateEvent, TEvent
    where TEvent : class, IAggregateEvent
{
    [Obsolete(ObsoleteMessage.ForInternalUseOnly)] void PublishInternal(TEventImplementation theEvent);
    [Obsolete(ObsoleteMessage.ForInternalUseOnly)] void ApplyEventInternal(TEvent @event);
    [Obsolete(ObsoleteMessage.ForInternalUseOnly)] IEventHandlerRegistrar<TEvent> RegisterEventAppliersInternal();
}
