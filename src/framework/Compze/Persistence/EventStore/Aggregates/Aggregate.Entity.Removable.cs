using System;
using Compze.Messaging.Events;

namespace Compze.Persistence.EventStore.Aggregates;

public partial class Aggregate<TAggregate, TAggregateEventImplementation, TAggregateEvent, TWrapperEventImplementation, TWrapperEventInterface>
    : IEventiveInternals<TAggregateEventImplementation, TAggregateEvent>
    where TWrapperEventImplementation : TWrapperEventInterface
    where TWrapperEventInterface : IAggregateWrapperEvent<TAggregateEvent>
    where TAggregate : Aggregate<TAggregate, TAggregateEventImplementation, TAggregateEvent, TWrapperEventImplementation, TWrapperEventInterface>
    where TAggregateEvent : class, IAggregateEvent
    where TAggregateEventImplementation : AggregateEvent, TAggregateEvent
{
    public abstract class AggregateRemovableEntity<TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityRemovedEvent, TEntityEventIdGetterSetter>
        : EventiveRemovableEntity<TAggregate, TAggregateEvent, TAggregateEventImplementation, TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityRemovedEvent, TEntityEventIdGetterSetter>
        where TEntityId : struct
        where TEntityEvent : class, TAggregateEvent
        where TEntityEventImplementation : TAggregateEventImplementation, TEntityEvent
        where TEntityCreatedEvent : TEntityEvent
        where TEntityRemovedEvent : TEntityEvent
        where TEntity : AggregateRemovableEntity<TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityRemovedEvent, TEntityEventIdGetterSetter>
        where TEntityEventIdGetterSetter : IGetSetAggregateEntityEventEntityId<TEntityId, TEntityEventImplementation, TEntityEvent>
    {
        static AggregateRemovableEntity() => AggregateTypeValidator<TEntity, TEntityEventImplementation, TEntityEvent>.AssertStaticStructureIsValid();

        protected AggregateRemovableEntity(TAggregate aggregate) : base(aggregate)
        {
        }
    }
}
