using System;
using System.Collections.Generic;
using Compze.Abstractions;
using Compze.Abstractions.Internal.GenericAbstractions.Time;
using Compze.EventStore.Abstractions;
using Compze.Tessaging.Common.Teventive;
using Compze.Teventive.Abstractions;
using Compze.Utilities.Contracts;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.SystemCE.ReactiveCE;
using JetBrains.Annotations;

namespace Compze.Teventive;

//Urgent:[Obsolete("Only here to let things compile while inheritors migrate to the version with 5 type parameters")]. Really? If you don't intend to inherit from the Aggregate, what good is it to set the last two type parameters so anything else?
public class Aggregate<TAggregate, TAggregateEvent, TAggregateEventImplementation> : Aggregate<TAggregate, TAggregateEvent, TAggregateEventImplementation, IAggregateWrapperEvent<TAggregateEvent>, AggregateWrapperEvent<TAggregateEvent>>
    where TAggregate : Aggregate<TAggregate, TAggregateEvent, TAggregateEventImplementation>
    where TAggregateEvent : class, IAggregateEvent
    where TAggregateEventImplementation : AggregateEvent, TAggregateEvent
{
    [Obsolete("Only for infrastructure", true)]
    protected Aggregate() : this(DateTimeNowTimeSource.Instance) {}

    protected Aggregate(IUtcTimeTimeSource timeSource) : base(timeSource) {}
}

public class Aggregate<TAggregate, TAggregateEvent, TAggregateEventImplementation, TWrapperEventInterface, TWrapperEventImplementation> :
    VersionedPersistentEntity<TAggregate>,
    IEventStored<TAggregateEvent>,
    IEventiveInternals<TAggregateEvent, TAggregateEventImplementation>
    where TWrapperEventImplementation : TWrapperEventInterface
    where TWrapperEventInterface : IAggregateWrapperEvent<TAggregateEvent>
    where TAggregate : Aggregate<TAggregate, TAggregateEvent, TAggregateEventImplementation, TWrapperEventInterface, TWrapperEventImplementation>
    where TAggregateEvent : class, IAggregateEvent
    where TAggregateEventImplementation : AggregateEvent, TAggregateEvent
{
    IUtcTimeTimeSource TimeSource { get; set; }

    static Aggregate() => AggregateTypeValidator<TAggregate, TAggregateEventImplementation, TAggregateEvent>.AssertStaticStructureIsValid();

    //Yes Guid.Empty. Id should be assigned by an action, and it should be obvious that the aggregate in invalid until that happens
    protected Aggregate(IUtcTimeTimeSource timeSource) : base(Guid.Empty)
    {
        Assert.Argument.NotNull(timeSource)
              .Is(typeof(TAggregateEvent).IsInterface);
        TimeSource = timeSource;
        _eventHandlersDispatcher.Register().IgnoreUnhandled<TAggregateEvent>();
    }

    readonly List<IAggregateEvent> _unCommittedEvents = [];
    readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateEvent> _eventAppliersDispatcher = new();
    readonly CallMatchingHandlersInRegistrationOrderEventDispatcher<TAggregateEvent> _eventHandlersDispatcher = new();

    int _reentrancyLevel;
    bool _applyingEvents;

    readonly List<TAggregateEventImplementation> _eventsPublishedDuringCurrentPublishCallIncludingReentrantCallsFromEventHandlers = [];

    protected TEvent Publish<TEvent>(TEvent theEvent) where TEvent : TAggregateEventImplementation
    {
        Assert.State.Is(!_applyingEvents, () => "You cannot raise events from within event appliers");

        using(ScopedChange.Enter(onEnter: () => _reentrancyLevel++, onDispose: () => _reentrancyLevel--))
        {
#pragma warning disable CS0618 // Type or member is obsolete
            ((IMutableAggregateEvent)theEvent).SetAggregateVersionInternal(Version + 1);
            ((IMutableAggregateEvent)theEvent).SetUtcTimeStampInternal(TimeSource.UtcNow);
            if(Version == 0)
            {
                if(theEvent is not IAggregateCreatedEvent) throw new Exception($"The first published event {theEvent.GetType()} did not implement {nameof(IAggregateCreatedEvent)}. The first event an aggregate publishes must always implement {nameof(IAggregateCreatedEvent)}.");
                if(theEvent.AggregateId == Guid.Empty) throw new Exception($"{nameof(IAggregateEvent.AggregateId)} was empty in {nameof(IAggregateCreatedEvent)}");
                ((IMutableAggregateEvent)theEvent).SetAggregateVersionInternal(1);
            } else
            {
                if(theEvent.AggregateId != Guid.Empty && theEvent.AggregateId != Id) throw new ArgumentOutOfRangeException($"Tried to raise event for Aggregated: {theEvent.AggregateId} from Aggregate with Id: {Id}.");
                ((IMutableAggregateEvent)theEvent).SetAggregateIdInternal(Id);
            }
#pragma warning restore CS0618 // Type or member is obsolete
            ApplyEvent(theEvent);
            _unCommittedEvents.Add(theEvent);
            _eventsPublishedDuringCurrentPublishCallIncludingReentrantCallsFromEventHandlers.Add(theEvent);
            _eventHandlersDispatcher.Dispatch(theEvent);
        }

        if(_reentrancyLevel == 0)
        {
            AssertInvariantsAreMet(); //It is allowed to enter a temporarily invalid state that will be corrected by new events published by event handlers. So we only check invariants once this event has been fully published including events published by handlers of the original event.
            foreach(var @event in _eventsPublishedDuringCurrentPublishCallIncludingReentrantCallsFromEventHandlers) _eventStream.OnNext(@event);
            _eventsPublishedDuringCurrentPublishCallIncludingReentrantCallsFromEventHandlers.Clear();
        }

        return theEvent;
    }

    protected IEventHandlerRegistrar<TAggregateEvent> RegisterEventAppliers() => _eventAppliersDispatcher.Register();

    // ReSharper disable once UnusedMember.Global todo: coverage
    protected IEventHandlerRegistrar<TAggregateEvent> RegisterEventHandlers() => _eventHandlersDispatcher.Register();

    void ApplyEvent(TAggregateEvent theEvent)
    {
        using(ScopedChange.Enter(onEnter: () => _applyingEvents = true, onDispose: () => _applyingEvents = false))
        {
            if(theEvent is IAggregateCreatedEvent)
            {
#pragma warning disable 618 // Review OK: This is the one place where we are quite sure that calling this obsolete method is correct.
                SetIdBeVerySureYouKnowWhatYouAreDoing(theEvent.AggregateId);
#pragma warning restore 618
            }

            Version = theEvent.AggregateVersion;
            _eventAppliersDispatcher.Dispatch(theEvent);
        }
    }

    protected virtual void AssertInvariantsAreMet() {}

    readonly SimpleObservable<TAggregateEventImplementation> _eventStream = new();
#pragma warning disable CA1033 //These method should NOT clutter the public interface of Aggregates.
    void IEventiveInternals<TAggregateEvent, TAggregateEventImplementation>.ApplyEventInternal(TAggregateEvent theEvent) => ApplyEvent(theEvent);
    void IEventiveInternals<TAggregateEvent, TAggregateEventImplementation>.PublishInternal(TAggregateEventImplementation theEvent) => Publish(theEvent);
    IEventHandlerRegistrar<TAggregateEvent> IEventiveInternals<TAggregateEvent, TAggregateEventImplementation>.RegisterEventAppliersInternal() => RegisterEventAppliers();

    IObservable<IAggregateEvent> IEventStored.EventStream => _eventStream;
    IObservable<TAggregateEvent> IEventStored<TAggregateEvent>.EventStream => _eventStream;

    void IEventStored.Commit(Action<IReadOnlyList<IAggregateEvent>> commitEvents)
    {
        commitEvents(_unCommittedEvents);
        _unCommittedEvents.Clear();
    }

    void IEventStored.SetTimeSource(IUtcTimeTimeSource timeSource) => TimeSource = timeSource;

    void IEventStored.LoadFromHistory(IEnumerable<IAggregateEvent> history)
    {
        Assert.State.Is(Version == 0, () => $"You can only call {nameof(IEventStored.LoadFromHistory)} on an empty Aggregate with {nameof(Version)} == 0");
        history.ForEach(theEvent => ApplyEvent((TAggregateEvent)theEvent));
        AssertInvariantsAreMet();
    }
#pragma warning restore CA1033

    public abstract class Component<TComponent, TComponentEventImplementation, TComponentEvent>
        : EventiveComponent<TAggregate, TAggregateEvent, TAggregateEventImplementation, TComponent, TComponentEvent, TComponentEventImplementation>
        where TComponentEvent : class, TAggregateEvent
        where TComponentEventImplementation : TAggregateEventImplementation, TComponentEvent
        where TComponent : Component<TComponent, TComponentEventImplementation, TComponentEvent>
    {
        protected Component(TAggregate parent) : base(parent) {}
    }

    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
    public abstract class Entity<TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityEventIdGetterSetter>
        : EventiveEntity<TAggregate, TAggregateEvent, TAggregateEventImplementation, TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityEventIdGetterSetter>
        where TEntityId : struct
        where TEntityEvent : class, TAggregateEvent
        where TEntityEventImplementation : TAggregateEventImplementation, TEntityEvent
        where TEntityCreatedEvent : TEntityEvent
        where TEntity : Entity<TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityEventIdGetterSetter>
        where TEntityEventIdGetterSetter : IGetSetAggregateEntityEventEntityId<TEntityId, TEntityEventImplementation, TEntityEvent>
    {
        protected Entity(TAggregate aggregate) : base(aggregate) {}
    }

    public abstract class RemovableEntity<TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityRemovedEvent, TEntityEventIdGetterSetter>
        : EventiveRemovableEntity<TAggregate, TAggregateEvent, TAggregateEventImplementation, TEntity, TEntityId, TEntityEvent, TEntityEventImplementation, TEntityCreatedEvent, TEntityRemovedEvent, TEntityEventIdGetterSetter>
        where TEntityId : struct
        where TEntityEvent : class, TAggregateEvent
        where TEntityEventImplementation : TAggregateEventImplementation, TEntityEvent
        where TEntityCreatedEvent : TEntityEvent
        where TEntityRemovedEvent : TEntityEvent
        where TEntity : RemovableEntity<TEntity, TEntityId, TEntityEventImplementation, TEntityEvent, TEntityCreatedEvent, TEntityRemovedEvent, TEntityEventIdGetterSetter>
        where TEntityEventIdGetterSetter : IGetSetAggregateEntityEventEntityId<TEntityId, TEntityEventImplementation, TEntityEvent>
    {
        protected RemovableEntity(TAggregate aggregate) : base(aggregate) {}
    }
}
