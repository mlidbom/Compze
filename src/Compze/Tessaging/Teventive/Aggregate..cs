using System;
using System.Collections.Generic;
using Compze.Abstractions.Public;
using Compze.Abstractions.Tessaging.Teventive.TeventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Abstractions.Time.Public;
using Compze.Utilities.Contracts;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.SystemCE.ReactiveCE;
using JetBrains.Annotations;

namespace Compze.Tessaging.Teventive;

//Urgent:[Obsolete("Only here to let things compile while inheritors migrate to the version with 5 type parameters")]. Really? If you don't intend to inherit from the Aggregate, what good is it to set the last two type parameters so anything else?
public class Aggregate<TAggregate, TAggregateTevent, TAggregateTeventImplementation> : Aggregate<TAggregate, TAggregateTevent, TAggregateTeventImplementation, IAggregateWrapperTevent<TAggregateTevent>, AggregateWrapperTevent<TAggregateTevent>>
    where TAggregate : Aggregate<TAggregate, TAggregateTevent, TAggregateTeventImplementation>
    where TAggregateTevent : class, IAggregateTevent
    where TAggregateTeventImplementation : AggregateTevent, TAggregateTevent
{
    [Obsolete("Only for infrastructure", true)]
    protected Aggregate() : this(DateTimeNowTimeSource.Instance) {}

    protected Aggregate(IUtcTimeTimeSource timeSource) : base(timeSource) {}
}

public class Aggregate<TAggregate, TAggregateTevent, TAggregateTeventImplementation, TWrapperTeventInterface, TWrapperTeventImplementation> :
    VersionedPersistentEntity<TAggregate>,
    ITeventStored<TAggregateTevent>,
    ITeventiveInternals<TAggregateTevent, TAggregateTeventImplementation>
    where TWrapperTeventImplementation : TWrapperTeventInterface
    where TWrapperTeventInterface : IAggregateWrapperTevent<TAggregateTevent>
    where TAggregate : Aggregate<TAggregate, TAggregateTevent, TAggregateTeventImplementation, TWrapperTeventInterface, TWrapperTeventImplementation>
    where TAggregateTevent : class, IAggregateTevent
    where TAggregateTeventImplementation : AggregateTevent, TAggregateTevent
{
    IUtcTimeTimeSource TimeSource { get; set; }

    static Aggregate() => AggregateTypeValidator<TAggregate, TAggregateTeventImplementation, TAggregateTevent>.AssertStaticStructureIsValid();

    //Yes Guid.Empty. Id should be assigned by an action, and it should be obvious that the aggregate in invalid until that happens
    protected Aggregate(IUtcTimeTimeSource timeSource) : base(Guid.Empty)
    {
        Assert.Argument.NotNull(timeSource)
              .Is(typeof(TAggregateTevent).IsInterface);
        TimeSource = timeSource;
        _teventHandlersDispatcher.Register().IgnoreUnhandled<TAggregateTevent>();
    }

    readonly List<IAggregateTevent> _unCommittedTevents = [];
    readonly IMutableTeventDispatcher<TAggregateTevent> _teventAppliersDispatcher = IMutableTeventDispatcher<TAggregateTevent>.New();
    readonly IMutableTeventDispatcher<TAggregateTevent> _teventHandlersDispatcher = IMutableTeventDispatcher<TAggregateTevent>.New();

    int _reentrancyLevel;
    bool _applyingTevents;

    readonly List<TAggregateTeventImplementation> _teventsPublishedDuringCurrentPublishCallIncludingReentrantCallsFromTeventHandlers = [];

    protected TTevent Publish<TTevent>(TTevent theTevent) where TTevent : TAggregateTeventImplementation
    {
        Assert.State.Is(!_applyingTevents, () => "You cannot raise tevents from within tevent appliers");

        using(ScopedChange.Enter(() => _reentrancyLevel++, () => _reentrancyLevel--))
        {
#pragma warning disable CS0618 // Type or member is obsolete
            ((IMutableAggregateTevent)theTevent).SetAggregateVersionInternal(Version + 1);
            ((IMutableAggregateTevent)theTevent).SetUtcTimeStampInternal(TimeSource.UtcNow);
            if(Version == 0)
            {
                if(theTevent is not IAggregateCreatedTevent) throw new Exception($"The first published tevent {theTevent.GetType()} did not implement {nameof(IAggregateCreatedTevent)}. The first tevent an aggregate publishes must always implement {nameof(IAggregateCreatedTevent)}.");
                if(theTevent.AggregateId == Guid.Empty) throw new Exception($"{nameof(IAggregateTevent.AggregateId)} was empty in {nameof(IAggregateCreatedTevent)}");
                ((IMutableAggregateTevent)theTevent).SetAggregateVersionInternal(1);
            } else
            {
                if(theTevent.AggregateId != Guid.Empty && theTevent.AggregateId != Id) throw new ArgumentOutOfRangeException($"Tried to raise tevent for Aggregated: {theTevent.AggregateId} from Aggregate with Id: {Id}.");
                ((IMutableAggregateTevent)theTevent).SetAggregateIdInternal(Id);
            }
#pragma warning restore CS0618 // Type or member is obsolete
            ApplyTevent(theTevent);
            _unCommittedTevents.Add(theTevent);
            _teventsPublishedDuringCurrentPublishCallIncludingReentrantCallsFromTeventHandlers.Add(theTevent);
            _teventHandlersDispatcher.Dispatch(theTevent);
        }

        if(_reentrancyLevel == 0)
        {
            AssertInvariantsAreMet(); //It is allowed to enter a temporarily invalid state that will be corrected by new tevents published by tevent handlers. So we only check invariants once this tevent has been fully published including tevents published by handlers of the original tevent.
            foreach(var @tevent in _teventsPublishedDuringCurrentPublishCallIncludingReentrantCallsFromTeventHandlers) _teventStream.OnNext(@tevent);
            _teventsPublishedDuringCurrentPublishCallIncludingReentrantCallsFromTeventHandlers.Clear();
        }

        return theTevent;
    }

    protected ITeventHandlerRegistrar<TAggregateTevent> RegisterTeventAppliers() => _teventAppliersDispatcher.Register();

    // ReSharper disable once UnusedMember.Global todo: coverage
    protected ITeventHandlerRegistrar<TAggregateTevent> RegisterTeventHandlers() => _teventHandlersDispatcher.Register();

    void ApplyTevent(TAggregateTevent theTevent)
    {
        using(ScopedChange.Enter(() => _applyingTevents = true, () => _applyingTevents = false))
        {
            if(theTevent is IAggregateCreatedTevent)
            {
#pragma warning disable 618 // Reviewed OK: This is the one place where we are quite sure that calling this obsolete method is correct.
                SetIdBeVerySureYouKnowWhatYouAreDoing(theTevent.AggregateId);
#pragma warning restore 618
            }

            Version = theTevent.AggregateVersion;
            _teventAppliersDispatcher.Dispatch(theTevent);
        }
    }

    protected virtual void AssertInvariantsAreMet() {}

    readonly SimpleObservable<TAggregateTeventImplementation> _teventStream = new();
#pragma warning disable CA1033 //These method should NOT clutter the public interface of Aggregates.
    void ITeventiveInternals<TAggregateTevent, TAggregateTeventImplementation>.ApplyTeventInternal(TAggregateTevent theTevent) => ApplyTevent(theTevent);
    void ITeventiveInternals<TAggregateTevent, TAggregateTeventImplementation>.PublishInternal(TAggregateTeventImplementation theTevent) => Publish(theTevent);
    ITeventHandlerRegistrar<TAggregateTevent> ITeventiveInternals<TAggregateTevent, TAggregateTeventImplementation>.RegisterTeventAppliersInternal() => RegisterTeventAppliers();

    IObservable<IAggregateTevent> ITeventStored.TeventStream => _teventStream;
    IObservable<TAggregateTevent> ITeventStored<TAggregateTevent>.TeventStream => _teventStream;

    void ITeventStored.Commit(Action<IReadOnlyList<IAggregateTevent>> commitTevents)
    {
        commitTevents(_unCommittedTevents);
        _unCommittedTevents.Clear();
    }

    void ITeventStored.SetTimeSource(IUtcTimeTimeSource timeSource) => TimeSource = timeSource;

    void ITeventStored.LoadFromHistory(IEnumerable<IAggregateTevent> history)
    {
        Assert.State.Is(Version == 0, () => $"You can only call {nameof(ITeventStored.LoadFromHistory)} on an empty Aggregate with {nameof(Version)} == 0");
        history.ForEach(theTevent => ApplyTevent((TAggregateTevent)theTevent));
        AssertInvariantsAreMet();
    }
#pragma warning restore CA1033

    public abstract class Component<TComponent, TComponentTeventImplementation, TComponentTevent>
        : TeventiveComponent<TAggregate, TAggregateTevent, TAggregateTeventImplementation, TComponent, TComponentTevent, TComponentTeventImplementation>
        where TComponentTevent : class, TAggregateTevent
        where TComponentTeventImplementation : TAggregateTeventImplementation, TComponentTevent
        where TComponent : Component<TComponent, TComponentTeventImplementation, TComponentTevent>
    {
        protected Component(TAggregate parent) : base(parent) {}
    }

    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
    public abstract class Entity<TEntity, TEntityId, TEntityTeventImplementation, TEntityTevent, TEntityCreatedTevent, TEntityTeventIdGetterSetter>
        : TeventiveEntity<TAggregate, TAggregateTevent, TAggregateTeventImplementation, TEntity, TEntityId, TEntityTeventImplementation, TEntityTevent, TEntityCreatedTevent, TEntityTeventIdGetterSetter>
        where TEntityId : struct
        where TEntityTevent : class, TAggregateTevent
        where TEntityTeventImplementation : TAggregateTeventImplementation, TEntityTevent
        where TEntityCreatedTevent : TEntityTevent
        where TEntity : Entity<TEntity, TEntityId, TEntityTeventImplementation, TEntityTevent, TEntityCreatedTevent, TEntityTeventIdGetterSetter>
        where TEntityTeventIdGetterSetter : IGetSetAggregateEntityTeventEntityId<TEntityId, TEntityTeventImplementation, TEntityTevent>
    {
        protected Entity(TAggregate aggregate) : base(aggregate) {}
    }

    public abstract class RemovableEntity<TEntity, TEntityId, TEntityTeventImplementation, TEntityTevent, TEntityCreatedTevent, TEntityRemovedTevent, TEntityTeventIdGetterSetter>
        : TeventiveRemovableEntity<TAggregate, TAggregateTevent, TAggregateTeventImplementation, TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityRemovedTevent, TEntityTeventIdGetterSetter>
        where TEntityId : struct
        where TEntityTevent : class, TAggregateTevent
        where TEntityTeventImplementation : TAggregateTeventImplementation, TEntityTevent
        where TEntityCreatedTevent : TEntityTevent
        where TEntityRemovedTevent : TEntityTevent
        where TEntity : RemovableEntity<TEntity, TEntityId, TEntityTeventImplementation, TEntityTevent, TEntityCreatedTevent, TEntityRemovedTevent, TEntityTeventIdGetterSetter>
        where TEntityTeventIdGetterSetter : IGetSetAggregateEntityTeventEntityId<TEntityId, TEntityTeventImplementation, TEntityTevent>
    {
        protected RemovableEntity(TAggregate aggregate) : base(aggregate) {}
    }
}
