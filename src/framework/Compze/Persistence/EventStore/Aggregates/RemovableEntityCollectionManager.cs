using System;
using Compze.Messaging.Events;

namespace Compze.Persistence.EventStore.Aggregates;

public class RemovableEntityCollectionManager<TParent,
                                              TParentEvent,
                                              TParentEventImplementation,
                                              TEntity,
                                              TEntityId,
                                              TEntityEvent,
                                              TEntityEventImplementation,
                                              TEntityCreatedEvent,
                                              TEntityRemovedEvent,
                                              TEntityEventIdGetterSetter>
    : EntityCollectionManager<TParent, TParentEvent, TParentEventImplementation, TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityEventIdGetterSetter>
    where TParent : IEventiveInternals<TParentEvent, TParentEventImplementation>
    where TParentEvent : class, IAggregateEvent
    where TParentEventImplementation : AggregateEvent, TParentEvent
    where TEntityId : notnull
    where TEntityEvent : class, TParentEvent
    where TEntityCreatedEvent : TEntityEvent
    where TEntityRemovedEvent : TEntityEvent
    where TEntityEventImplementation : TEntityEvent, TParentEventImplementation
    where TEntity : EventiveComponent<TParent, TParentEvent, TParentEventImplementation, TEntity, TEntityEventImplementation, TEntityEvent>
    where TEntityEventIdGetterSetter :
    IGetSetAggregateEntityEventEntityId<TEntityId, TEntityEventImplementation, TEntityEvent>
{
    protected RemovableEntityCollectionManager(TParent parent,
                                               Action<TEntityEventImplementation> raiseEventThroughParent,
                                               IEventHandlerRegistrar<TEntityEvent> appliersRegistrar)
        : base(parent, raiseEventThroughParent, appliersRegistrar)
    {
        appliersRegistrar.For<TEntityRemovedEvent>(e =>
        {
            var id = IdGetter.GetId(e);
            ManagedEntities.Remove(id);
        });
    }
}
