using System;
using Compze.Messaging.Events;

namespace Compze.Persistence.EventStore.Aggregates;

public abstract class EventiveRemovableEntity<TParent,
                                              TParentEvent,
                                              TParentEventImplementation,
                                              TEntity,
                                              TEntityId,
                                              TEntityEventImplementation,
                                              TEntityEvent,
                                              TEntityCreatedEvent,
                                              TEntityRemovedEvent,
                                              TEntityEventIdGetterSetter>
    : EventiveEntity<TParent, TParentEvent, TParentEventImplementation, TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityEventIdGetterSetter>
    where TParent : IEventiveInternals<TParentEvent, TParentEventImplementation>
    where TParentEvent : class, IAggregateEvent
    where TEntityId : struct
    where TEntityEvent : class, TParentEvent
    where TParentEventImplementation : AggregateEvent, TParentEvent
    where TEntityEventImplementation : TParentEventImplementation, TEntityEvent
    where TEntityCreatedEvent : TEntityEvent
    where TEntityRemovedEvent : TEntityEvent
    where TEntity : EventiveRemovableEntity<TParent, TParentEvent, TParentEventImplementation, TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityRemovedEvent, TEntityEventIdGetterSetter>
    where TEntityEventIdGetterSetter : IGetSetAggregateEntityEventEntityId<TEntityId, TEntityEventImplementation, TEntityEvent>
{
    static EventiveRemovableEntity() => AggregateTypeValidator<TEntity, TEntityEventImplementation, TEntityEvent>.AssertStaticStructureIsValid();

    protected EventiveRemovableEntity(TParent aggregate) : base(aggregate)
    {
        RegisterEventAppliers()
           .IgnoreUnhandled<TEntityRemovedEvent>();
    }

    public new static CollectionManager CreateSelfManagingCollection(TParent parent)
        => new(parent, @event => parent.Publish(@event), parent.RegisterEventAppliers());

    public new class CollectionManager : RemovableEntityCollectionManager<TParent, TParentEvent, TParentEventImplementation, TEntity, TEntityId, TEntityEvent, TEntityEventImplementation, TEntityCreatedEvent, TEntityRemovedEvent, TEntityEventIdGetterSetter>
    {
        internal CollectionManager(TParent parent, Action<TEntityEventImplementation> raiseEventThroughParent, IEventHandlerRegistrar<TEntityEvent> appliersRegistrar)
            : base(parent, raiseEventThroughParent, appliersRegistrar) {}
    }
}
