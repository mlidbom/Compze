using Compze.GenericAbstractions.Time;
using Compze.Messaging.Events;
using System;
using Compze.Messaging;

namespace Compze.Persistence.EventStore.Aggregates;

public abstract class EventiveComponent<TParent, TParentEvent, TParentEventImplementation, TComponent, TComponentEventImplementation, TComponentEvent>
    where TParentEvent : IEvent
    where TComponentEvent : class, TParentEvent
    where TComponentEventImplementation : TParentEventImplementation, TComponentEvent
    where TComponent : EventiveComponent<TParent, TParentEvent, TParentEventImplementation, TComponent, TComponentEventImplementation, TComponentEvent>
{
    static EventiveComponent() => AggregateTypeValidator<TComponent, TComponentEventImplementation, TComponentEvent>.AssertStaticStructureIsValid();

    readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentEvent> _eventAppliersEventDispatcher = new();

    //todo: I don't like this being internal rather than private
    internal readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TComponentEvent> _eventHandlersEventDispatcher = new();
    readonly Action<TComponentEventImplementation> _raiseEventThroughParent;

    //todo: I don't like this being internal rather than private
    protected IUtcTimeTimeSource TimeSource { get; set; }

    protected void ApplyEvent(TComponentEvent @event) => _eventAppliersEventDispatcher.Dispatch(@event);

    internal EventiveComponent(IUtcTimeTimeSource timeSource, Action<TComponentEventImplementation> raiseEventThroughParent, IEventHandlerRegistrar<TComponentEvent> appliersRegistrar, bool registerEventAppliers)
    {
        TimeSource = timeSource;
        _raiseEventThroughParent = raiseEventThroughParent;
        _eventHandlersEventDispatcher.Register()
                                     .IgnoreUnhandled<TComponentEvent>();

        if(registerEventAppliers)
        {
            appliersRegistrar
               .For<TComponentEvent>(ApplyEvent);
        }
    }

    protected virtual void Publish(TComponentEventImplementation @event)
    {
        _raiseEventThroughParent(@event);
        _eventHandlersEventDispatcher.Dispatch(@event);
    }

    protected IEventHandlerRegistrar<TComponentEvent> RegisterEventAppliers() => _eventAppliersEventDispatcher.Register();

    // ReSharper disable once UnusedMember.Global todo: tests
    protected IEventHandlerRegistrar<TComponentEvent> RegisterEventHandlers() => _eventHandlersEventDispatcher.Register();
}
