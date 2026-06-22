using Compze.Abstractions.Public;
using Compze.Abstractions.Time.Public;
using Compze.Contracts;
using Compze.Internals.SystemCE.ReactiveCE;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Teventive.Internal.Implementation;
using Compze.Teventive.Taggregates.Tevents.Public;

namespace Compze.Teventive.Taggregates.BaseClasses;

public partial class Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TWrapperTeventInterface, TWrapperTeventImplementation> :
   VersionedEntity<TTaggregate>,
   ITaggregate<TTaggregateTevent>,
   ITeventiveInternals<TTaggregateTevent, TTaggregateTeventImplementation>
   where TWrapperTeventImplementation : TWrapperTeventInterface
   where TWrapperTeventInterface : ITaggregateIdentifyingTevent<TTaggregateTevent>
   where TTaggregate : Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TWrapperTeventInterface, TWrapperTeventImplementation>
   where TTaggregateTevent : class, ITaggregateTevent
   where TTaggregateTeventImplementation : TaggregateTevent, TTaggregateTevent
{
   static Taggregate() => TaggregateTypeValidator<TTaggregate, TTaggregateTeventImplementation, TTaggregateTevent>.AssertStaticStructureIsValid();

   protected virtual Type WrapperTEventImplementation => typeof(TWrapperTeventImplementation);

   TWrapperTeventInterface WrapEvent(TTaggregateTevent tevent) =>
      (TWrapperTeventInterface)Constructor.ForGenericType(WrapperTEventImplementation)
                                          .WithArgument(tevent.GetType())
                                          .Invoke(tevent);

   //Yes null. Id should be assigned by an action, and it should be obvious that the taggregate in invalid until that happens. It's a bit ugly to declare Id as non-null, but a null value will never escape the property due to contract validation
   protected Taggregate() : this(null!)
   {
   }

   Taggregate(TaggregateId id) : base(id)
   {
      Argument.Assert(typeof(TTaggregateTevent).IsInterface);
      _teventHandlersDispatcher.Register().IgnoreUnhandled<TTaggregateTevent>();
   }

   EntityId IEntity.Id => Id;
   public override TaggregateId Id => (TaggregateId)base.Id;

   readonly List<ITaggregateTevent> _unCommittedTevents = [];
   readonly IMutableTeventDispatcher<TTaggregateTevent> _teventAppliersDispatcher = IMutableTeventDispatcher<TTaggregateTevent>.New();
   readonly IMutableTeventDispatcher<TTaggregateTevent> _teventHandlersDispatcher = IMutableTeventDispatcher<TTaggregateTevent>.New();

   int _reentrancyLevel;
   bool _applyingTevents;

   readonly List<TTaggregateTeventImplementation> _teventsPublishedDuringCurrentPublishCallIncludingReentrantCallsFromTeventHandlers = [];

   protected TTevent Publish<TTevent>(TTevent tevent) where TTevent : TTaggregateTeventImplementation
   {
      State.Assert(!_applyingTevents, () => "You cannot raise tevents from within tevent appliers");

      var wrapped = WrapEvent(tevent);

      using(ScopedChange.Enter(() => _reentrancyLevel++, () => _reentrancyLevel--))
      {
#pragma warning disable CS0618 // Type or member is obsolete
         ((IMutableTaggregateTevent)tevent).SetTaggregateVersionInternal(Version + 1);
         ((IMutableTaggregateTevent)tevent).SetUtcTimeStampInternal(UtcTimeSource.UtcNow);
         if(Version == 0)
         {
            if(tevent is not ITaggregateCreatedTevent) throw new Exception($"The first published tevent {tevent.GetType()} did not implement {nameof(ITaggregateCreatedTevent)}. The first tevent an taggregate publishes must always implement {nameof(ITaggregateCreatedTevent)}.");
            if(tevent.TaggregateId is null)
               throw new Exception($"{nameof(ITaggregateTevent.TaggregateId)} was null in {nameof(ITaggregateCreatedTevent)}");
            ((IMutableTaggregateTevent)tevent).SetTaggregateVersionInternal(1);
         } else
         {
            if(tevent.TaggregateId is not null && tevent.TaggregateId != Id) throw new ArgumentOutOfRangeException($"Tried to raise tevent for Taggregated: {tevent.TaggregateId} from Taggregate with Id: {Id}.");
            ((IMutableTaggregateTevent)tevent).SetTaggregateIdInternal(Id);
         }
#pragma warning restore CS0618 // Type or member is obsolete
         ApplyTevent(tevent);
         _unCommittedTevents.Add(tevent);
         _teventsPublishedDuringCurrentPublishCallIncludingReentrantCallsFromTeventHandlers.Add(tevent);
         _teventHandlersDispatcher.Dispatch(wrapped);
      }

      if(_reentrancyLevel == 0)
      {
         AssertInvariantsAreMetInternal(); //It is allowed to enter a temporarily invalid state that will be corrected by new tevents published by tevent handlers. So we only check invariants once this tevent has been fully published including tevents published by handlers of the original tevent.
         foreach(var it in _teventsPublishedDuringCurrentPublishCallIncludingReentrantCallsFromTeventHandlers) _teventStream.OnNext(it);
         _teventsPublishedDuringCurrentPublishCallIncludingReentrantCallsFromTeventHandlers.Clear();
      }

      return tevent;
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
#pragma warning disable CS0618 // Type or member is obsolete
            base.Id = theTevent.TaggregateId;
#pragma warning restore CS0618 // Type or member is obsolete
         }

         Version = theTevent.TaggregateVersion;
         _teventAppliersDispatcher.Dispatch(WrapEvent(theTevent));
      }
   }

   void AssertInvariantsAreMetInternal()
   {
      Invariant.Assert(Id != null, Id.Value != Guid.Empty);
      AssertInvariantsAreMet();
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

   void ITaggregate.LoadFromHistory(IEnumerable<ITaggregateTevent> history)
   {
      State.Assert(Version == 0, () => $"You can only call {nameof(ITaggregate.LoadFromHistory)} on an empty Taggregate with {nameof(Version)} == 0");
      history.ForEach(theTevent => ApplyTevent((TTaggregateTevent)theTevent));
      AssertInvariantsAreMetInternal();
   }
#pragma warning restore CA1033
}
