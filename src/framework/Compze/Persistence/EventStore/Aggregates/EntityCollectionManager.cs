using Compze.Messaging.Events;
using Compze.SystemCE.ReflectionCE;
using System;

namespace Compze.Persistence.EventStore.Aggregates;

public class EntityCollectionManager<TParent,
                                     TParentEvent,
                                     TParentEventImplementation,
                                     TEntity,
                                     TEntityId,
                                     TEntityEventImplementation,
                                     TEntityEvent,
                                     TEntityCreatedEvent,
                                     TEntityEventIdGetterSetter>
    : IEntityCollectionManager<TEntity, TEntityId, TEntityEvent, TEntityEventImplementation, TEntityCreatedEvent>
    where TParent : IEventiveInternals<TParentEvent, TParentEventImplementation>
    where TParentEvent : class, IAggregateEvent
    where TParentEventImplementation : AggregateEvent, TParentEvent
    where TEntityId : notnull
    where TEntityEvent : class, TParentEvent
    where TEntityCreatedEvent : TEntityEvent
    where TEntityEventImplementation : TEntityEvent, TParentEventImplementation
    where TEntity : EventiveComponent<TParent, TParentEvent, TParentEventImplementation, TEntity, TEntityEventImplementation, TEntityEvent>
    where TEntityEventIdGetterSetter : IGetAggregateEntityEventEntityId<TEntityEvent, TEntityId>
{
    protected static readonly TEntityEventIdGetterSetter IdGetter = Constructor.For<TEntityEventIdGetterSetter>.DefaultConstructor.Instance();

    protected EntityCollection<TEntity, TEntityId> ManagedEntities { get; }
    readonly Action<TEntityEventImplementation> _raiseEventThroughParent;

    protected EntityCollectionManager(TParent parent,
                                      Action<TEntityEventImplementation> raiseEventThroughParent,
                                      IEventHandlerRegistrar<TEntityEvent> appliersRegistrar)
    {
        ManagedEntities = [];
        _raiseEventThroughParent = raiseEventThroughParent;
        appliersRegistrar
           .For<TEntityCreatedEvent>(e =>
            {
                var entity = Constructor.For<TEntity>.WithArguments<TParent>.Instance(parent);
                ManagedEntities.Add(entity, IdGetter.GetId(e));
            })
           .For<TEntityEvent>(e => GetEntityAsEventiveInternals(e).ApplyEvent(e));
    }

    IEventiveInternals<TEntityEvent, TEntityEventImplementation> GetEntityAsEventiveInternals(TEntityEvent e) => ManagedEntities[IdGetter.GetId(e)];

    public IReadOnlyEntityCollection<TEntity, TEntityId> Entities => ManagedEntities;

    public TEntity AddByPublishing<TCreationEvent>(TCreationEvent creationEvent) where TCreationEvent : TEntityEventImplementation, TEntityCreatedEvent
    {
        _raiseEventThroughParent(creationEvent);
        var result = ManagedEntities.InCreationOrder[^1];
        return result;
    }
}