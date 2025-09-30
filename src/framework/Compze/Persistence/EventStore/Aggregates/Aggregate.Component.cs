using System;
using Compze.GenericAbstractions.Time;
using Compze.Messaging.Events;

namespace Compze.Persistence.EventStore.Aggregates;

public partial class Aggregate<TAggregate, TAggregateEventImplementation, TAggregateEvent, TWrapperEventImplementation, TWrapperEventInterface>
    where TWrapperEventImplementation : TWrapperEventInterface
    where TWrapperEventInterface : IAggregateWrapperEvent<TAggregateEvent>
    where TAggregate : Aggregate<TAggregate, TAggregateEventImplementation, TAggregateEvent, TWrapperEventImplementation, TWrapperEventInterface>
    where TAggregateEvent : class, IAggregateEvent
    where TAggregateEventImplementation : AggregateEvent, TAggregateEvent
{
    public abstract partial class Component<TComponent, TComponentEventImplementation, TComponentEvent>
        : EventiveComponent<TAggregate, TAggregateEvent, TAggregateEventImplementation, TComponent, TComponentEventImplementation, TComponentEvent>
        where TComponentEvent : class, TAggregateEvent
        where TComponentEventImplementation : TAggregateEventImplementation, TComponentEvent
        where TComponent : Component<TComponent, TComponentEventImplementation, TComponentEvent>
    {
        protected Component(IUtcTimeTimeSource timeSource,
                            Action<TComponentEventImplementation> raiseEventThroughParent,
                            IEventHandlerRegistrar<TComponentEvent> appliersRegistrar,
                            bool registerEventAppliers)
            : base(timeSource, raiseEventThroughParent, appliersRegistrar, registerEventAppliers) {}
    }
}
