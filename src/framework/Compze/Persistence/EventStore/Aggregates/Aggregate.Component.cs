using Compze.GenericAbstractions.Time;
using Compze.Messaging.Events;
using Compze.SystemCE.ReflectionCE;
using System;
using JetBrains.Annotations;

namespace Compze.Persistence.EventStore.Aggregates;

public partial class Aggregate<TAggregate, TAggregateEventImplementation, TAggregateEvent, TWrapperEventImplementation, TWrapperEventInterface>
    where TWrapperEventImplementation : TWrapperEventInterface
    where TWrapperEventInterface : IAggregateWrapperEvent<TAggregateEvent>
    where TAggregate : Aggregate<TAggregate, TAggregateEventImplementation, TAggregateEvent, TWrapperEventImplementation, TWrapperEventInterface>
    where TAggregateEvent : class, IAggregateEvent
    where TAggregateEventImplementation : AggregateEvent, TAggregateEvent
{
    public abstract class Component<TComponent, TComponentEventImplementation, TComponentEvent>
        : EventiveComponent<TAggregate, TAggregateEvent, TAggregateEventImplementation, TComponent, TComponentEventImplementation, TComponentEvent>
        where TComponentEvent : class, TAggregateEvent
        where TComponentEventImplementation : TAggregateEventImplementation, TComponentEvent
        where TComponent : Component<TComponent, TComponentEventImplementation, TComponentEvent>
    {
        protected Component(Action<TComponentEventImplementation> raiseEventThroughParent, IEventHandlerRegistrar<TComponentEvent> appliersRegistrar, bool registerEventAppliers) : base(raiseEventThroughParent, appliersRegistrar, registerEventAppliers) {}
    }
}
