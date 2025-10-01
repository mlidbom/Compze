using Compze.Messaging.Events;
using Compze.SystemCE.ReflectionCE;
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
    protected IEventHandlerRegistrar<TComponentEvent> RegisterEventAppliers() => _eventAppliersEventDispatcher.Register();

    ///////////////////////// Collection manager

    public class EntityCollectionManager<TEntity,
                                         TEntityId,
                                         TEntityEventImplementation,
                                         TEntityEvent,
                                         TEntityCreatedEvent,
                                         TEntityEventIdGetterSetter>
        : EntityCollectionManager<TParent, TParentEvent, TParentEventImplementation, TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityEventIdGetterSetter>
        where TEntityId : notnull
        where TEntityEvent : class, TParentEvent
        where TEntityCreatedEvent : TEntityEvent
        where TEntityEventImplementation : TEntityEvent, TParentEventImplementation
        where TEntity : EventiveComponent<TParent, TParentEvent, TParentEventImplementation, TEntity, TEntityEventImplementation, TEntityEvent>
        where TEntityEventIdGetterSetter : IGetAggregateEntityEventEntityId<TEntityEvent, TEntityId>
    {
        protected EntityCollectionManager(TParent parent,
                                          Action<TEntityEventImplementation> raiseEventThroughParent,
                                          IEventHandlerRegistrar<TEntityEvent> appliersRegistrar)
            : base(parent, raiseEventThroughParent, appliersRegistrar) {}
    }
}
