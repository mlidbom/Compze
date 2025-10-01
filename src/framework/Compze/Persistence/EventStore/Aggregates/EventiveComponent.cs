using Compze.Messaging.Events;
using System;

namespace Compze.Persistence.EventStore.Aggregates;

public abstract class EventiveComponent<TParent, TParentEvent, TParentEventImplementation, TComponent, TComponentEventImplementation, TComponentEvent>
    : IEventiveInternals<TComponentEventImplementation, TComponentEvent>
    where TParent : IEventiveInternals<TParentEventImplementation, TParentEvent>
    where TParentEvent : class, IAggregateEvent
    where TParentEventImplementation : AggregateEvent, TParentEvent
    where TComponentEvent : class, TParentEvent
    where TComponentEventImplementation : TParentEventImplementation, TComponentEvent
    where TComponent : EventiveComponent<TParent, TParentEvent, TParentEventImplementation, TComponent, TComponentEventImplementation, TComponentEvent>
{
    static EventiveComponent() => AggregateTypeValidator<TComponent, TComponentEventImplementation, TComponentEvent>.AssertStaticStructureIsValid();

    readonly IMutableEventDispatcher<TComponentEvent> _eventAppliersEventDispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentEvent>();

    readonly Action<TComponentEventImplementation> _raiseEventThroughParent;
    readonly TParent _parent;

    void IEventiveInternals<TComponentEventImplementation, TComponentEvent>.ApplyEvent(TComponentEvent @event) => ApplyEvent(@event);
    protected void ApplyEvent(TComponentEvent @event) => _eventAppliersEventDispatcher.Dispatch(@event);

    protected EventiveComponent(TParent parent) : this(@event => parent.Publish(@event), parent.RegisterEventAppliers(), true)
    {
        _parent = parent;
    }

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

    public abstract class Component<TEcComponent,
                                      TEcComponentEventImplementation,
                                      TEcComponentEvent> :
        EventiveComponent<TComponent, TComponentEvent, TComponentEventImplementation, TEcComponent, TEcComponentEventImplementation, TEcComponentEvent>
        where TEcComponentEvent : class, TComponentEvent
        where TEcComponentEventImplementation : TComponentEventImplementation, TEcComponentEvent
        where TEcComponent : Component<TEcComponent, TEcComponentEventImplementation, TEcComponentEvent>
    {
        protected Component(TComponent parent): base(parent) { }
    }

    protected IEventHandlerRegistrar<TComponentEvent> RegisterEventAppliers() => _eventAppliersEventDispatcher.Register();

    public abstract class Entity<TEntity,
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
        where TEntity : Entity<TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityEventIdGetterSetter>
        where TEntityEventIdGetterSetter : IGetSetAggregateEntityEventEntityId<TEntityId, TEntityEventImplementation, TEntityEvent>
    {
        protected Entity(TComponent aggregate) : base(aggregate) {}
    }

    public abstract class RemovableEntity<TEntity,
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
        where TEntity : RemovableEntity<TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityRemovedEvent, TEntityEventIdGetterSetter>
        where TEntityEventIdGetterSetter : IGetSetAggregateEntityEventEntityId<TEntityId, TEntityEventImplementation, TEntityEvent>
    {
        protected RemovableEntity(TComponent aggregate) : base(aggregate) {}
    }
}
