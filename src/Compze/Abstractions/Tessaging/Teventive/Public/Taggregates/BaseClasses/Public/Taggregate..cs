using System;
using System.Collections.Generic;
using Compze.Abstractions.Public;
using Compze.Abstractions.Tessaging.Teventive.Internal.Implementation;
using Compze.Abstractions.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Abstractions.Time.Public;
using Compze.Utilities.Contracts;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.SystemCE.ReactiveCE;
using JetBrains.Annotations;

namespace Compze.Abstractions.Tessaging.Teventive.Public.Taggregates.BaseClasses.Public;

//Urgent:[Obsolete("Only here to let things compile while inheritors migrate to the version with 5 type parameters")]. Really? If you don't intend to inherit from the Taggregate, what good is it to set the last two type parameters so anything else?
public class Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation> : Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, ITaggregateWrapperTevent<TTaggregateTevent>, TaggregateWrapperTevent<TTaggregateTevent>>
    where TTaggregate : Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation>
    where TTaggregateTevent : class, ITaggregateTevent
    where TTaggregateTeventImplementation : TaggregateTevent, TTaggregateTevent
{
    [Obsolete("Only for infrastructure", true)]
    protected Taggregate() : this(DateTimeNowTimeSource.Instance) {}

    protected Taggregate(IUtcTimeTimeSource timeSource) : base(timeSource) {}
}

public class Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TWrapperTeventInterface, TWrapperTeventImplementation> :
    VersionedPersistentEntity<TTaggregate>,
    ITaggregate<TTaggregateTevent>,
    ITeventiveInternals<TTaggregateTevent, TTaggregateTeventImplementation>
    where TWrapperTeventImplementation : TWrapperTeventInterface
    where TWrapperTeventInterface : ITaggregateWrapperTevent<TTaggregateTevent>
    where TTaggregate : Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TWrapperTeventInterface, TWrapperTeventImplementation>
    where TTaggregateTevent : class, ITaggregateTevent
    where TTaggregateTeventImplementation : TaggregateTevent, TTaggregateTevent
{
    IUtcTimeTimeSource TimeSource { get; set; }

    static Taggregate() => TaggregateTypeValidator<TTaggregate, TTaggregateTeventImplementation, TTaggregateTevent>.AssertStaticStructureIsValid();

    //Yes Guid.Empty. Id should be assigned by an action, and it should be obvious that the taggregate in invalid until that happens
    protected Taggregate(IUtcTimeTimeSource timeSource) : base(Guid.Empty)
    {
        Assert.Argument.NotNull(timeSource)
              .Is(typeof(TTaggregateTevent).IsInterface);
        TimeSource = timeSource;
        _teventHandlersDispatcher.Register().IgnoreUnhandled<TTaggregateTevent>();
    }

    readonly List<ITaggregateTevent> _unCommittedTevents = [];
    readonly IMutableTeventDispatcher<TTaggregateTevent> _teventAppliersDispatcher = IMutableTeventDispatcher<TTaggregateTevent>.New();
    readonly IMutableTeventDispatcher<TTaggregateTevent> _teventHandlersDispatcher = IMutableTeventDispatcher<TTaggregateTevent>.New();

    int _reentrancyLevel;
    bool _applyingTevents;

    readonly List<TTaggregateTeventImplementation> _teventsPublishedDuringCurrentPublishCallIncludingReentrantCallsFromTeventHandlers = [];

    protected TTevent Publish<TTevent>(TTevent theTevent) where TTevent : TTaggregateTeventImplementation
    {
        Assert.State.Is(!_applyingTevents, () => "You cannot raise tevents from within tevent appliers");

        using(ScopedChange.Enter(() => _reentrancyLevel++, () => _reentrancyLevel--))
        {
#pragma warning disable CS0618 // Type or member is obsolete
            ((IMutableTaggregateTevent)theTevent).SetTaggregateVersionInternal(Version + 1);
            ((IMutableTaggregateTevent)theTevent).SetUtcTimeStampInternal(TimeSource.UtcNow);
            if(Version == 0)
            {
                if(theTevent is not ITaggregateCreatedTevent) throw new Exception($"The first published tevent {theTevent.GetType()} did not implement {nameof(ITaggregateCreatedTevent)}. The first tevent an taggregate publishes must always implement {nameof(ITaggregateCreatedTevent)}.");
                if(theTevent.TaggregateId == Guid.Empty) throw new Exception($"{nameof(ITaggregateTevent.TaggregateId)} was empty in {nameof(ITaggregateCreatedTevent)}");
                ((IMutableTaggregateTevent)theTevent).SetTaggregateVersionInternal(1);
            } else
            {
                if(theTevent.TaggregateId != Guid.Empty && theTevent.TaggregateId != Id) throw new ArgumentOutOfRangeException($"Tried to raise tevent for Taggregated: {theTevent.TaggregateId} from Taggregate with Id: {Id}.");
                ((IMutableTaggregateTevent)theTevent).SetTaggregateIdInternal(Id);
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

    protected ITeventHandlerRegistrar<TTaggregateTevent> RegisterTeventAppliers() => _teventAppliersDispatcher.Register();

    // ReSharper disable once UnusedMember.Global todo: coverage
    protected ITeventHandlerRegistrar<TTaggregateTevent> RegisterTeventHandlers() => _teventHandlersDispatcher.Register();

    void ApplyTevent(TTaggregateTevent theTevent)
    {
        using(ScopedChange.Enter(() => _applyingTevents = true, () => _applyingTevents = false))
        {
            if(theTevent is ITaggregateCreatedTevent)
            {
#pragma warning disable 618 // Reviewed OK: This is the one place where we are quite sure that calling this obsolete method is correct.
                SetIdBeVerySureYouKnowWhatYouAreDoing(theTevent.TaggregateId);
#pragma warning restore 618
            }

            Version = theTevent.TaggregateVersion;
            _teventAppliersDispatcher.Dispatch(theTevent);
        }
    }

    protected virtual void AssertInvariantsAreMet() {}

    readonly SimpleObservable<TTaggregateTeventImplementation> _teventStream = new();
#pragma warning disable CA1033 //These method should NOT clutter the public interface of Taggregates.
    void ITeventiveInternals<TTaggregateTevent, TTaggregateTeventImplementation>.ApplyTeventInternal(TTaggregateTevent theTevent) => ApplyTevent(theTevent);
    void ITeventiveInternals<TTaggregateTevent, TTaggregateTeventImplementation>.PublishInternal(TTaggregateTeventImplementation theTevent) => Publish(theTevent);
    ITeventHandlerRegistrar<TTaggregateTevent> ITeventiveInternals<TTaggregateTevent, TTaggregateTeventImplementation>.RegisterTeventAppliersInternal() => RegisterTeventAppliers();

    IObservable<ITaggregateTevent> ITaggregate.TeventStream => _teventStream;
    IObservable<TTaggregateTevent> ITaggregate<TTaggregateTevent>.TeventStream => _teventStream;

    void ITaggregate.Commit(Action<IReadOnlyList<ITaggregateTevent>> commitTevents)
    {
        commitTevents(_unCommittedTevents);
        _unCommittedTevents.Clear();
    }

    void ITaggregate.SetTimeSource(IUtcTimeTimeSource timeSource) => TimeSource = timeSource;

    void ITaggregate.LoadFromHistory(IEnumerable<ITaggregateTevent> history)
    {
        Assert.State.Is(Version == 0, () => $"You can only call {nameof(ITaggregate.LoadFromHistory)} on an empty Taggregate with {nameof(Version)} == 0");
        history.ForEach(theTevent => ApplyTevent((TTaggregateTevent)theTevent));
        AssertInvariantsAreMet();
    }
#pragma warning restore CA1033

    public abstract class Component<TComponent, TComponentTeventImplementation, TComponentTevent>
        : TeventiveComponent<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TComponent, TComponentTevent, TComponentTeventImplementation>
        where TComponentTevent : class, TTaggregateTevent
        where TComponentTeventImplementation : TTaggregateTeventImplementation, TComponentTevent
        where TComponent : Component<TComponent, TComponentTeventImplementation, TComponentTevent>
    {
        protected Component(TTaggregate parent) : base(parent) {}
    }

    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
    public abstract class Entity<TEntity, TEntityId, TEntityTeventImplementation, TEntityTevent, TEntityCreatedTevent, TEntityTeventIdGetterSetter>
        : TeventiveEntity<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TEntity, TEntityId, TEntityTeventImplementation, TEntityTevent, TEntityCreatedTevent, TEntityTeventIdGetterSetter>
        where TEntityId : struct
        where TEntityTevent : class, TTaggregateTevent
        where TEntityTeventImplementation : TTaggregateTeventImplementation, TEntityTevent
        where TEntityCreatedTevent : TEntityTevent
        where TEntity : Entity<TEntity, TEntityId, TEntityTeventImplementation, TEntityTevent, TEntityCreatedTevent, TEntityTeventIdGetterSetter>
        where TEntityTeventIdGetterSetter : IGetSetTaggregateEntityTeventEntityId<TEntityId, TEntityTeventImplementation, TEntityTevent>
    {
        protected Entity(TTaggregate taggregate) : base(taggregate) {}
    }

    public abstract class RemovableEntity<TEntity, TEntityId, TEntityTeventImplementation, TEntityTevent, TEntityCreatedTevent, TEntityRemovedTevent, TEntityTeventIdGetterSetter>
        : TeventiveRemovableEntity<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TEntity, TEntityId, TEntityTevent, TEntityTeventImplementation, TEntityCreatedTevent, TEntityRemovedTevent, TEntityTeventIdGetterSetter>
        where TEntityId : struct
        where TEntityTevent : class, TTaggregateTevent
        where TEntityTeventImplementation : TTaggregateTeventImplementation, TEntityTevent
        where TEntityCreatedTevent : TEntityTevent
        where TEntityRemovedTevent : TEntityTevent
        where TEntity : RemovableEntity<TEntity, TEntityId, TEntityTeventImplementation, TEntityTevent, TEntityCreatedTevent, TEntityRemovedTevent, TEntityTeventIdGetterSetter>
        where TEntityTeventIdGetterSetter : IGetSetTaggregateEntityTeventEntityId<TEntityId, TEntityTeventImplementation, TEntityTevent>
    {
        protected RemovableEntity(TTaggregate taggregate) : base(taggregate) {}
    }
}
