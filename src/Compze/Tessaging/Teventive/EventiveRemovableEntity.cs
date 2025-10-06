using Compze.EventStore.Abstractions;

namespace Compze.Teventive;

public abstract class EventiveRemovableEntity<TParent,
                                              TParentEvent,
                                              TParentEventImplementation,
                                              TEntity,
                                              TEntityId,
                                              TEntityEvent,
                                              TEntityEventImplementation,
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
    where TEntity : EventiveRemovableEntity<TParent, TParentEvent, TParentEventImplementation, TEntity, TEntityId, TEntityEvent, TEntityEventImplementation, TEntityCreatedEvent, TEntityRemovedEvent, TEntityEventIdGetterSetter>
    where TEntityEventIdGetterSetter : IGetSetAggregateEntityEventEntityId<TEntityId, TEntityEventImplementation, TEntityEvent>
{
    static EventiveRemovableEntity() => AggregateTypeValidator<TEntity, TEntityEventImplementation, TEntityEvent>.AssertStaticStructureIsValid();

    protected EventiveRemovableEntity(TParent aggregate) : base(aggregate)
    {
        RegisterEventAppliers()
           .IgnoreUnhandled<TEntityRemovedEvent>();
    }

    public new static CollectionManager CreateSelfManagingCollection(TParent parent)
        => new(parent);

    public new class CollectionManager : EventiveEntity<TParent, TParentEvent, TParentEventImplementation, TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityEventIdGetterSetter>.CollectionManager
    {
        internal CollectionManager(TParent parent): base(parent)
        {
#pragma warning disable 618 //Review OK: This test class is allowed to use these "obsolete" methods.
            parent.RegisterEventAppliersInternal().For<TEntityRemovedEvent>(e =>
            {
#pragma warning restore 618
                var id = IdGetter.GetId(e);
                ManagedEntities.Remove(id);
            });
        }
    }
}
