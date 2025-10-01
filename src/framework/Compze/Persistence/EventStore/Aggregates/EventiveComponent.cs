using Compze.Messaging.Events;
using System;

namespace Compze.Persistence.EventStore.Aggregates;

public abstract class EventiveComponent<TParent, TParentEvent, TParentEventImplementation, TComponent, TComponentEventImplementation, TComponentEvent>
    : IEventiveInternals<TComponentEventImplementation, TComponentEvent>
    where TParentEvent : class, IAggregateEvent
    where TParentEventImplementation : AggregateEvent, TParentEvent
    where TComponentEvent : class, TParentEvent
    where TComponentEventImplementation : TParentEventImplementation, TComponentEvent
    where TComponent : EventiveComponent<TParent, TParentEvent, TParentEventImplementation, TComponent, TComponentEventImplementation, TComponentEvent>
{
    static EventiveComponent() => AggregateTypeValidator<TComponent, TComponentEventImplementation, TComponentEvent>.AssertStaticStructureIsValid();

    readonly IMutableEventDispatcher<TComponentEvent> _eventAppliersEventDispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentEvent>();

    readonly Action<TComponentEventImplementation> _raiseEventThroughParent;

    void IEventiveInternals<TComponentEventImplementation, TComponentEvent>.ApplyEvent(TComponentEvent @event) => ApplyEvent(@event);
    protected void ApplyEvent(TComponentEvent @event) => _eventAppliersEventDispatcher.Dispatch(@event);

    internal EventiveComponent(Action<TComponentEventImplementation> raiseEventThroughParent, IEventHandlerRegistrar<TComponentEvent> appliersRegistrar, bool registerEventAppliers)
    {
        _raiseEventThroughParent = raiseEventThroughParent;

        if(registerEventAppliers)
        {
            appliersRegistrar
               .For<TComponentEvent>(ApplyEvent);
        }
    }

    void IEventiveInternals<TComponentEventImplementation, TComponentEvent>.Publish(TComponentEventImplementation @event) => Publish(@event);

    protected virtual void Publish(TComponentEventImplementation @event) => _raiseEventThroughParent(@event);

    IEventHandlerRegistrar<TComponentEvent> IEventiveInternals<TComponentEventImplementation, TComponentEvent>.RegisterEventAppliers() => RegisterEventAppliers();

    public abstract class EcComponent<TEcComponent,
                                      TEcComponentEventImplementation,
                                      TEcComponentEvent> :
        EventiveComponent<TComponent, TComponentEvent, TComponentEventImplementation, TEcComponent, TEcComponentEventImplementation, TEcComponentEvent>
        where TEcComponentEvent : class, TComponentEvent
        where TEcComponentEventImplementation : TComponentEventImplementation, TEcComponentEvent
        where TEcComponent : EcComponent<TEcComponent, TEcComponentEventImplementation, TEcComponentEvent>
    {
        protected EcComponent(Action<TEcComponentEventImplementation> raiseEventThroughParent,
                              IEventHandlerRegistrar<TEcComponentEvent> appliersRegistrar,
                              bool registerEventAppliers)
            : base(raiseEventThroughParent, appliersRegistrar, registerEventAppliers) {}
    }

    protected IEventHandlerRegistrar<TComponentEvent> RegisterEventAppliers() => _eventAppliersEventDispatcher.Register();

    public abstract class EcEntity<TEntity,
                                   TEntityId,
                                   TEntityEventImplementation,
                                   TEntityEvent,
                                   TEntityCreatedEvent,
                                   TEntityEventIdGetterSetter> :
        EventiveEntity<
            TComponent,
            TComponentEvent,
            TComponentEventImplementation,
            TEntity,
            TEntityId,
            TEntityEventImplementation,
            TEntityEvent,
            TEntityCreatedEvent,
            TEntityEventIdGetterSetter>
        where TEntityId : struct
        where TEntityEvent : class, TComponentEvent
        where TEntityEventImplementation : TComponentEventImplementation, TEntityEvent
        where TEntityCreatedEvent : TEntityEvent
        where TEntity : EcEntity<TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityEventIdGetterSetter>
        where TEntityEventIdGetterSetter : IGetSetAggregateEntityEventEntityId<TEntityId, TEntityEventImplementation, TEntityEvent>
    {
        protected EcEntity(TComponent aggregate) : base(aggregate) {}
        protected EcEntity(Action<TEntityEventImplementation> raiseEventThroughParent, IEventHandlerRegistrar<TEntityEvent> appliersRegistrar) : base(raiseEventThroughParent, appliersRegistrar) {}
    }

    public abstract class EcRemovableEntity<TEntity,
                                            TEntityId,
                                            TEntityEventImplementation,
                                            TEntityEvent,
                                            TEntityCreatedEvent,
                                            TEntityRemovedEvent,
                                            TEntityEventIdGetterSetter> :
        EventiveRemovableEntity<
            TComponent,
            TComponentEvent,
            TComponentEventImplementation,
            TEntity,
            TEntityId,
            TEntityEventImplementation,
            TEntityEvent,
            TEntityCreatedEvent,
            TEntityRemovedEvent,
            TEntityEventIdGetterSetter>
        where TEntityId : struct
        where TEntityEvent : class, TComponentEvent
        where TEntityEventImplementation : TComponentEventImplementation, TEntityEvent
        where TEntityCreatedEvent : TEntityEvent
        where TEntityRemovedEvent : TEntityEvent
        where TEntity : EcRemovableEntity<TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityRemovedEvent, TEntityEventIdGetterSetter>
        where TEntityEventIdGetterSetter : IGetSetAggregateEntityEventEntityId<TEntityId, TEntityEventImplementation, TEntityEvent>
    {
        protected EcRemovableEntity(TComponent aggregate) : base(aggregate) {}
    }
}
