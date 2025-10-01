using Compze.Messaging.Events;
using System;

namespace Compze.Persistence.EventStore.Aggregates;

public abstract partial class EventiveComponent<TParent, TParentEvent, TParentEventImplementation, TComponent, TComponentEventImplementation, TComponentEvent>
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

    readonly IMutableEventDispatcher<TComponentEvent> _eventHandlersEventDispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentEvent>();
    protected IEventDispatcher<TComponentEvent> EventHandlersEventDispatcher => _eventHandlersEventDispatcher;
    readonly Action<TComponentEventImplementation> _raiseEventThroughParent;

    protected void ApplyEvent(TComponentEvent @event) => _eventAppliersEventDispatcher.Dispatch(@event);

    internal EventiveComponent(Action<TComponentEventImplementation> raiseEventThroughParent, IEventHandlerRegistrar<TComponentEvent> appliersRegistrar, bool registerEventAppliers)
    {
        _raiseEventThroughParent = raiseEventThroughParent;
        _eventHandlersEventDispatcher.Register()
                                     .IgnoreUnhandled<TComponentEvent>();

        if(registerEventAppliers)
        {
            appliersRegistrar
               .For<TComponentEvent>(ApplyEvent);
        }
    }

    void IEventiveInternals<TComponentEventImplementation, TComponentEvent>.Publish(TComponentEventImplementation @event) => Publish(@event);

    protected virtual void Publish(TComponentEventImplementation @event)
    {
        _raiseEventThroughParent(@event);
        EventHandlersEventDispatcher.Dispatch(@event);
    }

    IEventHandlerRegistrar<TComponentEvent> IEventiveInternals<TComponentEventImplementation, TComponentEvent>.RegisterEventAppliers() => RegisterEventAppliers();
    protected IEventHandlerRegistrar<TComponentEvent> RegisterEventAppliers() => _eventAppliersEventDispatcher.Register();

    // ReSharper disable once UnusedMember.Global todo: tests
    protected IEventHandlerRegistrar<TComponentEvent> RegisterEventHandlers() => _eventHandlersEventDispatcher.Register();
}
