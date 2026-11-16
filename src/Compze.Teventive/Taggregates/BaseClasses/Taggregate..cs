using Compze.Teventive._private;
using Compze.Abstractions;
using Compze.Abstractions.Time;
using Compze.Contracts;
using Compze.Internals.SystemCE.ReactiveCE;
using Compze.Tessaging.TessageTypes;
using Compze.Teventive._private.Implementation;
using Compze.Teventive.Taggregates.Tevents;

namespace Compze.Teventive.Taggregates.BaseClasses;

public partial class Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TWrapperTeventInterface, TWrapperTeventImplementation> :
   VersionedEntity<TTaggregate>,
   ITaggregate<TTaggregateTevent>,
   ITeventiveInternals<TTaggregateTevent, TTaggregateTeventImplementation>
   where TWrapperTeventImplementation : TWrapperTeventInterface
   where TWrapperTeventInterface : ITaggregateTevent<TTaggregateTevent>
   where TTaggregate : Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TWrapperTeventInterface, TWrapperTeventImplementation>
   where TTaggregateTevent : class, ITaggregateTevent
   where TTaggregateTeventImplementation : TaggregateTevent, TTaggregateTevent
{
   static Taggregate() => TaggregateTypeValidator<TTaggregate, TTaggregateTeventImplementation, TTaggregateTevent>.AssertStaticStructureIsValid();

   protected virtual Type WrapperTeventImplementation => typeof(TWrapperTeventImplementation);

   TWrapperTeventInterface WrapTevent(TTaggregateTevent tevent) =>
      (TWrapperTeventInterface)TaggregateIdentifyingTevent.WrapIn(WrapperTeventImplementation, tevent);

   //Yes null id passed to base. Id should be assigned by an action, and it should be obvious that the taggregate in invalid until that happens. It's a bit ugly to declare Id as non-null, but a null value will never escape the property due to contract validation
   protected Taggregate(TeventDispatcherConfig? teventAppliersDispatcherConfig = null) : base(null!)
   {
      Argument.Assert(typeof(TTaggregateTevent).IsInterface);
      _teventAppliersDispatcherConfig = teventAppliersDispatcherConfig;
   }

   ///<summary>The routing of this taggregate's class to its tevent appliers, when the class declares one: a <see cref="TeventRouting"/> in a static field.<br/>
   /// Null for a class that registers its appliers per instance, through <see cref="RegisterTeventAppliers"/>.</summary>
   protected virtual TeventRouting? TeventAppliers => ConventionTeventRouting.AppliersOf(GetType());

   ///<summary>The routing of this taggregate's class to its tevent handlers, when the class declares one. See <see cref="TeventAppliers"/>.</summary>
   protected virtual TeventRouting? TeventHandlers => ConventionTeventRouting.HandlersOf(GetType());

   EntityId IEntity.Id => Id;
   public override TaggregateId Id => (TaggregateId)base.Id;

   readonly List<ITaggregateTevent<TTaggregateTevent>> _unCommittedTevents = [];
   //wip: taggregate-experiments-opus keeps the per-instance dispatchers for the classes that declare no TeventRouting, created when first registered on.
   readonly TeventDispatcherConfig? _teventAppliersDispatcherConfig;
   IMutableTeventDispatcher<TTaggregateTevent>? _teventAppliersDispatcher;
   IMutableTeventDispatcher<TTaggregateTevent>? _teventHandlersDispatcher;

   IMutableTeventDispatcher<TTaggregateTevent> TeventAppliersDispatcher => _teventAppliersDispatcher ??= IMutableTeventDispatcher<TTaggregateTevent>.New(_teventAppliersDispatcherConfig);

   int _reentrancyLevel;
   bool _applyingTevents;

   readonly List<TWrapperTeventInterface> _teventsPublishedDuringCurrentPublishCallIncludingReentrantCallsFromTeventHandlers = [];

   protected TTevent Publish<TTevent>(TTevent tevent) where TTevent : TTaggregateTeventImplementation
   {
      State.Assert(!_applyingTevents, () => "You cannot raise tevents from within tevent appliers");

      var wrapped = WrapTevent(tevent);

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
         ApplyTevent(tevent, wrapped);
         _unCommittedTevents.Add(wrapped);
         _teventsPublishedDuringCurrentPublishCallIncludingReentrantCallsFromTeventHandlers.Add(wrapped);
         TeventHandlers?.Dispatch(this, wrapped);
         _teventHandlersDispatcher?.Dispatch(wrapped);
      }

      if(_reentrancyLevel == 0)
      {
         AssertInvariantsAreMetInternal(); //It is allowed to enter a temporarily invalid state that will be corrected by new tevents published by tevent handlers. So we only check invariants once this tevent has been fully published including tevents published by handlers of the original tevent.
         foreach(var it in _teventsPublishedDuringCurrentPublishCallIncludingReentrantCallsFromTeventHandlers) _teventStream.OnNext(it);
         _teventsPublishedDuringCurrentPublishCallIncludingReentrantCallsFromTeventHandlers.Clear();
      }

      return tevent;
   }

   protected ITeventSubscriber<TTaggregateTevent> RegisterTeventAppliers()
   {
      State.Assert(TeventAppliers is null, () => $"{GetType()} declares its tevent appliers in a {nameof(TeventRouting)}, so it cannot also register appliers per instance.");
      return TeventAppliersDispatcher.Register();
   }

   protected ITeventSubscriber<TTaggregateTevent> RegisterTeventHandlers()
   {
      State.Assert(TeventHandlers is null, () => $"{GetType()} declares its tevent handlers in a {nameof(TeventRouting)}, so it cannot also register handlers per instance.");
      return (_teventHandlersDispatcher ??= IMutableTeventDispatcher<TTaggregateTevent>.New(TeventDispatcherConfig.IgnoreAllUnhandled)).Register(); //Registering tevent handlers is optional, unlike tevent appliers.
   }

   void ApplyTevent(TTaggregateTevent theTevent) => ApplyTevent(theTevent, WrapTevent(theTevent));

   void ApplyTevent(TTaggregateTevent theTevent, IPublisherTevent<TTaggregateTevent> wrapped)
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
         if(TeventAppliers is {} appliers) appliers.Dispatch(this, wrapped);
         else TeventAppliersDispatcher.Dispatch(wrapped);
      }
   }

   void AssertInvariantsAreMetInternal()
   {
      Invariant.Assert(Id != null, Id.Value != Guid.Empty);
      AssertInvariantsAreMet();
   }

   protected virtual void AssertInvariantsAreMet() {}

   readonly SimpleObservable<ITaggregateTevent<TTaggregateTevent>> _teventStream = new();
#pragma warning disable CA1033 //These method should NOT clutter the public interface of Taggregates.
   void ITeventiveInternals<TTaggregateTevent, TTaggregateTeventImplementation>.ApplyTeventInternal(TTaggregateTevent theTevent) => ApplyTevent(theTevent);
   void ITeventiveInternals<TTaggregateTevent, TTaggregateTeventImplementation>.PublishInternal(TTaggregateTeventImplementation theTevent) => Publish(theTevent);
   ITeventSubscriber<TTaggregateTevent> ITeventiveInternals<TTaggregateTevent, TTaggregateTeventImplementation>.RegisterTeventAppliersInternal() => RegisterTeventAppliers();
   bool ITeventiveInternals<TTaggregateTevent, TTaggregateTeventImplementation>.DeclaresTeventAppliersInternal => TeventAppliers is not null;

   IObservable<ITaggregateTevent<ITaggregateTevent>> ITaggregate.TeventStream => _teventStream;
   IObservable<ITaggregateTevent<TTaggregateTevent>> ITaggregate<TTaggregateTevent>.TeventStream => _teventStream;

   void ITaggregate.Commit(Action<IReadOnlyList<ITaggregateTevent<ITaggregateTevent>>> commitTevents)
   {
      commitTevents(_unCommittedTevents);
      _unCommittedTevents.Clear();
   }

   void ITaggregate.LoadFromHistory(IEnumerable<ITaggregateTevent<ITaggregateTevent>> history)
   {
      State.Assert(Version == 0, () => $"You can only call {nameof(ITaggregate.LoadFromHistory)} on an empty Taggregate with {nameof(Version)} == 0");
      //The stored wrapper is applied as it stands: after a migration has rewritten history the stored wrapper is the truth, not what this taggregate would wrap today.
      history.ForEach(wrappedTevent => ApplyTevent((TTaggregateTevent)wrappedTevent.Tevent, (IPublisherTevent<TTaggregateTevent>)wrappedTevent));
      AssertInvariantsAreMetInternal();
   }
#pragma warning restore CA1033
}
