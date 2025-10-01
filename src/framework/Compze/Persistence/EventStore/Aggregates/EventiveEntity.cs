using Compze.Contracts;
using Compze.Messaging.Events;
using Compze.SystemCE.ReflectionCE;
using JetBrains.Annotations;
using System;

namespace Compze.Persistence.EventStore.Aggregates;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
public abstract class EventiveEntity<TParent,
                                     TParentEvent,
                                     TParentEventImplementation,
                                     TEntity,
                                     TEntityId,
                                     TEntityEventImplementation,
                                     TEntityEvent,
                                     TEntityCreatedEvent,
                                     TEntityEventIdGetterSetter>
    : EventiveComponent<TParent, TParentEvent, TParentEventImplementation, TEntity, TEntityEvent, TEntityEventImplementation>
    where TParent : IEventiveInternals<TParentEvent, TParentEventImplementation>
    where TParentEvent : class, IAggregateEvent
    where TEntityId : struct
    where TEntityEvent : class, TParentEvent
    where TParentEventImplementation : AggregateEvent, TParentEvent
    where TEntityEventImplementation : TParentEventImplementation, TEntityEvent
    where TEntityCreatedEvent : TEntityEvent
    where TEntity : EventiveEntity<TParent, TParentEvent, TParentEventImplementation, TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityEventIdGetterSetter>
    where TEntityEventIdGetterSetter : IGetSetAggregateEntityEventEntityId<TEntityId, TEntityEventImplementation, TEntityEvent>
{
    static EventiveEntity() => AggregateTypeValidator<TEntity, TEntityEventImplementation, TEntityEvent>.AssertStaticStructureIsValid();

    static readonly TEntityEventIdGetterSetter IdGetterSetter = Constructor.For<TEntityEventIdGetterSetter>.DefaultConstructor.Instance();

    TEntityId _id;
    public TEntityId Id => Assert.Result.ReturnNotDefault(_id);

    protected EventiveEntity(TParent aggregate) : base(aggregate, false)
    {
        RegisterEventAppliers()
           .For<TEntityCreatedEvent>(e => _id = IdGetterSetter.GetId(e));
    }

    protected override void Publish(TEntityEventImplementation @event)
    {
        var id = IdGetterSetter.GetId(@event);
        if(Equals(id, default(TEntityId)))
        {
            IdGetterSetter.SetEntityId(@event, Id);
        } else if(!Equals(id, Id))
        {
            throw new Exception($"Attempted to raise event with EntityId: {id} from within entity with EntityId: {Id}");
        }

        base.Publish(@event);
    }

    // ReSharper disable once UnusedMember.Global todo: write tests.
    public static CollectionManager CreateSelfManagingCollection(TParent parent) => new(parent);

    public class CollectionManager : IEntityCollectionManager<TEntity, TEntityId, TEntityEvent, TEntityEventImplementation, TEntityCreatedEvent>
    {
        protected static readonly TEntityEventIdGetterSetter IdGetter = Constructor.For<TEntityEventIdGetterSetter>.DefaultConstructor.Instance();

        protected EntityCollection<TEntity, TEntityId> ManagedEntities { get; }

        TParent _parent;

        internal CollectionManager(TParent parent)
        {
            ManagedEntities = [];
            _parent = parent;
            parent.RegisterEventAppliers()
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
            _parent.Publish(creationEvent);
            var result = ManagedEntities.InCreationOrder[^1];
            return result;
        }
    }
}
