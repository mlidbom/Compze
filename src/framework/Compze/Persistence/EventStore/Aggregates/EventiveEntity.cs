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
    : EventiveComponent<TParent, TParentEvent, TParentEventImplementation, TEntity, TEntityEventImplementation, TEntityEvent>,
      IEventiveInternals<TEntityEventImplementation, TEntityEvent>
    where TParent : IEventiveInternals<TParentEventImplementation, TParentEvent>
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

    protected EventiveEntity(TParent aggregate)
        : this(@event => aggregate.Publish(@event), aggregate.RegisterEventAppliers()) { }

    protected EventiveEntity(Action<TEntityEventImplementation> raiseEventThroughParent,
                             IEventHandlerRegistrar<TEntityEvent> appliersRegistrar)
        : base(raiseEventThroughParent, appliersRegistrar, registerEventAppliers: false)
    {
        RegisterEventAppliers()
           .For<TEntityCreatedEvent>(e => _id = IdGetterSetter.GetId(e));
    }

    void IEventiveInternals<TEntityEventImplementation, TEntityEvent>.ApplyEvent(TEntityEvent @event) => ApplyEvent(@event);
    IEventHandlerRegistrar<TEntityEvent> IEventiveInternals<TEntityEventImplementation, TEntityEvent>.RegisterEventAppliers() => RegisterEventAppliers();

    void IEventiveInternals<TEntityEventImplementation, TEntityEvent>.Publish(TEntityEventImplementation @event) => Publish(@event);

    protected override void Publish(TEntityEventImplementation @event)
    {
        var id = IdGetterSetter.GetId(@event);
        if(Equals(id, default(TEntityId)))
        {
            IdGetterSetter.SetEntityId(@event, Id);
        } else if(!Equals(id, Id))
        {
            throw new Exception($"Attempted to raise event with EntityId: {id} frow within entity with EntityId: {Id}");
        }

        base.Publish(@event);
    }

    // ReSharper disable once UnusedMember.Global todo: write tests.
    public static CollectionManager CreateSelfManagingCollection(TParent parent) => new(parent, @event => parent.Publish(@event), parent.RegisterEventAppliers());

    public class CollectionManager : EntityCollectionManager<TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityEventIdGetterSetter>
    {
        internal CollectionManager(TParent parent, Action<TEntityEventImplementation> raiseEventThroughParent, IEventHandlerRegistrar<TEntityEvent> appliersRegistrar)
            : base(parent, raiseEventThroughParent, appliersRegistrar) {}
    }
}

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
    where TParent : IEventiveInternals<TParentEventImplementation, TParentEvent>
    where TParentEvent : class, IAggregateEvent
    where TEntityId : struct
    where TEntityEvent : class, TParentEvent
    where TParentEventImplementation : AggregateEvent, TParentEvent
    where TEntityEventImplementation : TParentEventImplementation, TEntityEvent
    where TEntityCreatedEvent : TEntityEvent
    where TEntityRemovedEvent : TEntityEvent
    where TEntity : EventiveRemovableEntity<TParent, TParentEvent, TParentEventImplementation,TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityRemovedEvent, TEntityEventIdGetterSetter>
    where TEntityEventIdGetterSetter : IGetSetAggregateEntityEventEntityId<TEntityId, TEntityEventImplementation, TEntityEvent>
{
    static EventiveRemovableEntity() => AggregateTypeValidator<TEntity, TEntityEventImplementation, TEntityEvent>.AssertStaticStructureIsValid();

    protected EventiveRemovableEntity(TParent aggregate) : base(aggregate)
    {
        RegisterEventAppliers()
           .IgnoreUnhandled<TEntityRemovedEvent>();
    }

    public new static CollectionManager CreateSelfManagingCollection(TParent parent)
        => new(parent: parent, raiseEventThroughParent: @event => parent.Publish(@event), appliersRegistrar: parent.RegisterEventAppliers());

    public new class CollectionManager : RemovableEntityCollectionManager<TParent, TParentEvent, TParentEventImplementation, TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityRemovedEvent, TEntityEventIdGetterSetter>
    {
        internal CollectionManager(TParent parent, Action<TEntityEventImplementation> raiseEventThroughParent, IEventHandlerRegistrar<TEntityEvent> appliersRegistrar)
            : base(parent, raiseEventThroughParent, appliersRegistrar) {}
    }
}
