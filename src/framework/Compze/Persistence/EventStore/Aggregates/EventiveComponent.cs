using Compze.Messaging.Events;
using System;

namespace Compze.Persistence.EventStore.Aggregates;

public abstract class EventiveComponent<TParent,
                                        TParentEvent,
                                        TParentEventImplementation,
                                        TComponent,
                                        TComponentEvent,
                                        TComponentEventImplementation>
    : IEventiveInternals<TComponentEvent, TComponentEventImplementation>
    where TParent : IEventiveInternals<TParentEvent, TParentEventImplementation>
    where TParentEvent : class, IAggregateEvent
    where TParentEventImplementation : AggregateEvent, TParentEvent
    where TComponentEvent : class, TParentEvent
    where TComponentEventImplementation : TParentEventImplementation, TComponentEvent
    where TComponent : EventiveComponent<TParent, TParentEvent, TParentEventImplementation, TComponent, TComponentEvent, TComponentEventImplementation>
{
    static EventiveComponent() => AggregateTypeValidator<TComponent, TComponentEventImplementation, TComponentEvent>.AssertStaticStructureIsValid();

    readonly IMutableEventDispatcher<TComponentEvent> _eventAppliersEventDispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentEvent>();

    protected TParent _parent;

    void IEventiveInternals<TComponentEvent, TComponentEventImplementation>.ApplyEvent(TComponentEvent @event) => ApplyEvent(@event);
    protected void ApplyEvent(TComponentEvent @event) => _eventAppliersEventDispatcher.Dispatch(@event);

    protected EventiveComponent(TParent parent, bool registerEventAppliers = true)
    {
        _parent = parent;
        if(registerEventAppliers)
        {
            parent.RegisterEventAppliers()
                  .For<TComponentEvent>(ApplyEvent);
        }
    }

    void IEventiveInternals<TComponentEvent, TComponentEventImplementation>.Publish(TComponentEventImplementation @event) => Publish(@event);

    protected virtual void Publish(TComponentEventImplementation @event) => _parent.Publish(@event);

    IEventHandlerRegistrar<TComponentEvent> IEventiveInternals<TComponentEvent, TComponentEventImplementation>.RegisterEventAppliers() => RegisterEventAppliers();

    public abstract class Component<TEcComponent,
                                    TEcComponentEventImplementation,
                                    TEcComponentEvent> :
        EventiveComponent<TComponent,
            TComponentEvent,
            TComponentEventImplementation,
            TEcComponent,
            TEcComponentEvent,
            TEcComponentEventImplementation>
        where TEcComponentEvent : class, TComponentEvent
        where TEcComponentEventImplementation : TComponentEventImplementation, TEcComponentEvent
        where TEcComponent : Component<TEcComponent, TEcComponentEventImplementation, TEcComponentEvent>
    {
        protected Component(TComponent parent) : base(parent) {}
    }

    protected IEventHandlerRegistrar<TComponentEvent> RegisterEventAppliers() => _eventAppliersEventDispatcher.Register();

    public abstract class Entity<TEntity,
                                 TEntityId,
                                 TEntityEvent,
                                 TEntityEventImplementation,
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
        where TEntity : Entity<TEntity, TEntityId, TEntityEvent, TEntityEventImplementation, TEntityCreatedEvent, TEntityEventIdGetterSetter>
        where TEntityEventIdGetterSetter : IGetSetAggregateEntityEventEntityId<TEntityId, TEntityEventImplementation, TEntityEvent>
    {
        protected Entity(TComponent aggregate) : base(aggregate) {}
    }

    public abstract class RemovableEntity<TEntity,
                                          TEntityId,
                                          TEntityEvent,
                                          TEntityEventImplementation,
                                          TEntityCreatedEvent,
                                          TEntityRemovedEvent,
                                          TEntityEventIdGetterSetter> :
        EventiveRemovableEntity<
            TComponent,
            TComponentEvent,
            TComponentEventImplementation,
            TEntity,
            TEntityId,
            TEntityEvent,
            TEntityEventImplementation,
            TEntityCreatedEvent,
            TEntityRemovedEvent,
            TEntityEventIdGetterSetter>
        where TEntityId : struct
        where TEntityEvent : class, TComponentEvent
        where TEntityEventImplementation : TComponentEventImplementation, TEntityEvent
        where TEntityCreatedEvent : TEntityEvent
        where TEntityRemovedEvent : TEntityEvent
        where TEntity : RemovableEntity<TEntity, TEntityId, TEntityEvent, TEntityEventImplementation, TEntityCreatedEvent, TEntityRemovedEvent, TEntityEventIdGetterSetter>
        where TEntityEventIdGetterSetter : IGetSetAggregateEntityEventEntityId<TEntityId, TEntityEventImplementation, TEntityEvent>
    {
        protected RemovableEntity(TComponent aggregate) : base(aggregate) {}
    }
}
