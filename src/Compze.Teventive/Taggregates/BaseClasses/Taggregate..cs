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
      _teventAppliersDispatcher = IMutableTeventDispatcher<TTaggregateTevent>.New(teventAppliersDispatcherConfig);
   }

   EntityId IEntity.Id => Id;
   public override TaggregateId Id => (TaggregateId)base.Id;

   ///<summary>The tevents published since the last <see cref="ITaggregate.Commit"/>, or null while there are none: created by the first, and handed on whole<br/>
   /// by the commit, so that a taggregate without uncommitted tevents keeps no list.</summary>
   List<ITaggregateTevent<TTaggregateTevent>>? _unCommittedTevents;
   readonly IMutableTeventDispatcher<TTaggregateTevent> _teventAppliersDispatcher;
   readonly IMutableTeventDispatcher<TTaggregateTevent> _teventHandlersDispatcher = IMutableTeventDispatcher<TTaggregateTevent>.New(TeventDispatcherConfig.IgnoreAllUnhandled); //Registering tevent handlers is optional, unlike tevent appliers.

   int _reentrancyLevel;
   bool _applyingTevents;

   ///<summary>The managed thread id of the thread using this taggregate, or 0 while none is: see <see cref="OneThreadAtATime"/>.</summary>
   int _idOfTheThreadUsingIt;

   ///<summary>The tevents of the publish cascade under way: the tevent <see cref="Publish{TTevent}"/> was called with, and every tevent that tevent handlers<br/>
   /// published from inside it. Handed to <see cref="OnPublishCascadeEnded"/> when the cascade ends.</summary>
   List<ITaggregateTevent<TTaggregateTevent>>? _teventsOfThePublishCascadeUnderWay;

   ///<summary>Publishes <paramref name="tevent"/>: applies it, keeps it uncommitted, and calls the tevent handlers, which may publish more tevents. The tevent<br/>
   /// and those the handlers publish, from inside this call, are one publish cascade: once it has ended the invariants are asserted, and<br/>
   /// <see cref="OnPublishCascadeEnded"/> is called with its tevents.</summary>
   protected TTevent Publish<TTevent>(TTevent tevent) where TTevent : TTaggregateTeventImplementation
   {
      State.Assert(!_applyingTevents, () => "You cannot raise tevents from within tevent appliers");
      using var oneThreadAtATime = OneThreadAtATime.Enter(ref _idOfTheThreadUsingIt, this);

      if(_reentrancyLevel > 0)
      {
         PublishInThePublishCascadeUnderWay(tevent);
         return tevent;
      }

      IReadOnlyList<ITaggregateTevent<TTaggregateTevent>> teventsOfTheCascade;
      try
      {
         PublishInThePublishCascadeUnderWay(tevent);
      }
      finally
      {
         //Taken before the cascade is handed on, so that a publish made by whatever it's handed to starts a publish cascade of its own instead of changing
         //this one; and taken when the cascade failed too, so that its tevents don't go out with the next one.
         teventsOfTheCascade = TakeAll(ref _teventsOfThePublishCascadeUnderWay);
      }

      AssertInvariantsAreMetInternal(); //It is allowed to enter a temporarily invalid state that will be corrected by new tevents published by tevent handlers. So we only check invariants once the whole publish cascade has been published.
      OnPublishCascadeEnded(teventsOfTheCascade);
      return tevent;
   }

   void PublishInThePublishCascadeUnderWay(TTaggregateTeventImplementation tevent)
   {
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
         (_unCommittedTevents ??= []).Add(wrapped);
         (_teventsOfThePublishCascadeUnderWay ??= []).Add(wrapped);
         _teventHandlersDispatcher.Dispatch(wrapped);
      }
   }

   ///<summary>Called once at the end of each publish cascade, after the invariants are asserted, with the cascade's tevents in the order they were<br/>
   /// published, each in its publisher's wrapper. The base implementation emits them on <see cref="ITaggregate.TeventStream"/>.</summary>
   ///<remarks>A publish made from here, on this taggregate, starts a publish cascade of its own, which calls this again when it ends; and<br/>
   /// <see cref="ITaggregate.Commit"/> may be called from here. A nested cascade's call can end before this one does, so an override that persists<br/>
   /// should persist before it calls anything that may publish.</remarks>
   protected virtual void OnPublishCascadeEnded(IReadOnlyList<ITaggregateTevent<TTaggregateTevent>> teventsOfTheCascade)
   {
      if(_teventStream is null) return;
      foreach(var it in teventsOfTheCascade) _teventStream.OnNext(it);
   }

   protected ITeventSubscriber<TTaggregateTevent> RegisterTeventAppliers() => _teventAppliersDispatcher.Register();

   protected ITeventSubscriber<TTaggregateTevent> RegisterTeventHandlers() => _teventHandlersDispatcher.Register();

   void ApplyTevent(TTaggregateTevent theTevent) => ApplyTevent(theTevent, WrapTevent(theTevent));

   void ApplyTevent(TTaggregateTevent theTevent, IPublisherTevent<TTaggregateTevent> wrapped)
   {
      using(ScopedChange.Enter(() => _applyingTevents = true, () => _applyingTevents = false))
      {
         if(theTevent is ITaggregateCreatedTevent or ITaggregateSnapshotTevent)
         {
#pragma warning disable CS0618 // Type or member is obsolete
            base.Id = theTevent.TaggregateId;
#pragma warning restore CS0618 // Type or member is obsolete
         }

         Version = theTevent.TaggregateVersion;
         _teventAppliersDispatcher.Dispatch(wrapped);
      }
   }

   void AssertInvariantsAreMetInternal()
   {
      Invariant.Assert(Id != null, Id.Value != Guid.Empty);
      AssertInvariantsAreMet();
   }

   protected virtual void AssertInvariantsAreMet() {}

   ///<summary>Created when first subscribed to, so that a taggregate that nobody observes carries no observable.</summary>
   SimpleObservable<ITaggregateTevent<TTaggregateTevent>>? _teventStream;

   SimpleObservable<ITaggregateTevent<TTaggregateTevent>> TeventStreamCreatedWhenFirstUsed => LazyInitializer.EnsureInitialized(ref _teventStream);

   ///<summary>Hands on the list <paramref name="tevents"/> refers to, whole, and leaves it referring to none: the next tevent starts a new list, so that no<br/>
   /// one changes the list that is handed on.</summary>
   static IReadOnlyList<ITaggregateTevent<TTaggregateTevent>> TakeAll(ref List<ITaggregateTevent<TTaggregateTevent>>? tevents)
   {
      IReadOnlyList<ITaggregateTevent<TTaggregateTevent>> taken = tevents ?? [];
      tevents = null;
      return taken;
   }

#pragma warning disable CA1033 //These method should NOT clutter the public interface of Taggregates.
   void ITeventiveInternals<TTaggregateTevent, TTaggregateTeventImplementation>.ApplyTeventInternal(TTaggregateTevent theTevent) => ApplyTevent(theTevent);
   void ITeventiveInternals<TTaggregateTevent, TTaggregateTeventImplementation>.PublishInternal(TTaggregateTeventImplementation theTevent) => Publish(theTevent);
   ITeventSubscriber<TTaggregateTevent> ITeventiveInternals<TTaggregateTevent, TTaggregateTeventImplementation>.RegisterTeventAppliersInternal() => RegisterTeventAppliers();

   IObservable<ITaggregateTevent<ITaggregateTevent>> ITaggregate.TeventStream => TeventStreamCreatedWhenFirstUsed;
   IObservable<ITaggregateTevent<TTaggregateTevent>> ITaggregate<TTaggregateTevent>.TeventStream => TeventStreamCreatedWhenFirstUsed;

   void ITaggregate.Commit(Action<IReadOnlyList<ITaggregateTevent<ITaggregateTevent>>> commitTevents)
   {
      using var oneThreadAtATime = OneThreadAtATime.Enter(ref _idOfTheThreadUsingIt, this);
      //Taken before they are handed on, so that a publish made by whatever they're handed to stays uncommitted, for a later commit, instead of changing the
      //list being handed on.
      commitTevents(TakeAll(ref _unCommittedTevents));
   }

   void ITaggregate.LoadFromHistory(IEnumerable<ITaggregateTevent<ITaggregateTevent>> history)
   {
      using var oneThreadAtATime = OneThreadAtATime.Enter(ref _idOfTheThreadUsingIt, this);
      State.Assert(Version == 0, () => $"You can only call {nameof(ITaggregate.LoadFromHistory)} on an empty Taggregate with {nameof(Version)} == 0");
      //The stored wrapper is applied as it stands: after a migration has rewritten history the stored wrapper is the truth, not what this taggregate would wrap today.
      history.ForEach(wrappedTevent => ApplyTevent((TTaggregateTevent)wrappedTevent.Tevent, (IPublisherTevent<TTaggregateTevent>)wrappedTevent));
      AssertInvariantsAreMetInternal();
   }

   void ITaggregate.LoadFromSnapshot(IEnumerable<ITaggregateTevent> snapshot, int version)
   {
      using var oneThreadAtATime = OneThreadAtATime.Enter(ref _idOfTheThreadUsingIt, this);
      State.Assert(Version == 0, () => $"You can only call {nameof(ITaggregate.LoadFromSnapshot)} on an empty Taggregate with {nameof(Version)} == 0");
      Argument.Assert(version > 0, () => $"A snapshot stands for the first {nameof(version)} tevents of a taggregate, at least one: {version}");

      var tevents = snapshot.ToList();
      Argument.Assert(tevents.Count > 0, () => "A snapshot has at least one tevent: the one that restores the taggregate's own state and establishes its identity.");
      Argument.Assert(tevents[0] is ITaggregateSnapshotTevent, () => $"A snapshot starts with an {nameof(ITaggregateSnapshotTevent)}, which establishes the taggregate's identity. It started with a {tevents[0].GetType()}.");
      Argument.Assert(tevents.Skip(1).None(it => it is ITaggregateSnapshotTevent), () => $"Only the first tevent of a snapshot is an {nameof(ITaggregateSnapshotTevent)}.");

      var id = tevents[0].TaggregateId;
      Argument.Assert(id is not null, () => $"The {nameof(ITaggregateSnapshotTevent)} establishes the taggregate's identity, so it must carry its {nameof(ITaggregateTevent.TaggregateId)}.");

      foreach(var tevent in tevents)
      {
         Argument.Assert(tevent is TTaggregateTeventImplementation, () => $"Every tevent of a snapshot of a {typeof(TTaggregate)} is a {typeof(TTaggregateTeventImplementation)}: {tevent.GetType()} is not.");
         Argument.Assert(tevent.TaggregateId is null || tevent.TaggregateId == id, () => $"Every tevent of a snapshot belongs to the taggregate the snapshot restores, {id}: a {tevent.GetType()} carried {tevent.TaggregateId}.");
#pragma warning disable CS0618 // Type or member is obsolete
         ((IMutableTaggregateTevent)tevent).SetTaggregateIdInternal(id);
         ((IMutableTaggregateTevent)tevent).SetTaggregateVersionInternal(version);
#pragma warning restore CS0618 // Type or member is obsolete
         ApplyTevent((TTaggregateTevent)tevent);
      }

      AssertInvariantsAreMetInternal();
   }
#pragma warning restore CA1033
}
