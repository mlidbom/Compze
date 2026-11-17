using Compze.Abstractions;
using Compze.Abstractions.Time;
using Compze.Contracts;
using Compze.Internals.SystemCE.ReactiveCE;
using Compze.Tessaging.TessageTypes;
using Compze.Teventive._internal;
using Compze.Teventive._private.Implementation;
using Compze.Teventive.Taggregates.BaseClasses._private;
using Compze.Teventive.Taggregates.Tevents;

namespace Compze.Teventive.Taggregates.BaseClasses;

public partial class Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TWrapperTeventInterface, TWrapperTeventImplementation> :
   TeventApplyingRoot<TTaggregate, TTaggregateTevent>,
   ITaggregate<TTaggregateTevent>,
   ITeventiveInternals<TTaggregateTevent, TTaggregateTeventImplementation>,
   ITaggregateStoreInternals,
   ITeventiveInATaggregate
   where TWrapperTeventImplementation : TWrapperTeventInterface
   where TWrapperTeventInterface : ITaggregateTevent<TTaggregateTevent>
   where TTaggregate : Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TWrapperTeventInterface, TWrapperTeventImplementation>
   where TTaggregateTevent : class, ITaggregateTevent
   where TTaggregateTeventImplementation : TaggregateTevent, TTaggregateTevent
{
   static Taggregate() => TaggregateTypeValidator<TTaggregate, TTaggregateTeventImplementation, TTaggregateTevent>.AssertStaticStructureIsValid();

   ///<summary>Only <typeparamref name="TTaggregate"/> constructs its base class.</summary>
   protected Taggregate() {}

   protected virtual Type WrapperTeventImplementation => typeof(TWrapperTeventImplementation);

   TWrapperTeventInterface WrapTevent(TTaggregateTevent tevent) =>
      (TWrapperTeventInterface)TaggregateIdentifyingTevent.WrapIn(WrapperTeventImplementation, tevent);

   ///<summary>The routing of this taggregate's class to its tevent handlers, when the class declares one, read through <see cref="ITeventRouted"/>.</summary>
   TeventRouting? TeventHandlers => ((ITeventRouted)this).TeventHandlers;

   ///<summary>Declares the tevent handlers of <typeparamref name="TTaggregate"/>, for it to keep in a static field and return from its explicit<br/>
   /// implementation of <see cref="ITeventRouted.TeventHandlers"/>. They run after every applier has applied the tevent, and a tevent may reach none.</summary>
   protected static TeventRouting<TTaggregate, TTaggregateTevent> DeclareHandlers(Action<TeventRouting<TTaggregate, TTaggregateTevent>.Declaration> declare) =>
      TeventRouting<TTaggregate, TTaggregateTevent>.DeclareHandlers(declare);

   EntityId IEntity.Id => Id;
   public override TaggregateId Id => (TaggregateId)base.Id;

   readonly List<ITaggregateTevent<TTaggregateTevent>> _unCommittedTevents = [];

   int _reentrancyLevel;
   bool _applyingTevents;
   bool _isReadOnly;

   ///<summary>Whether this instance is being restored from a snapshot: from <see cref="RestoreIdentity"/> until the store that restores it<br/>
   /// has its restoring code return, or else until its first publish.</summary>
   bool _isBeingRestored;
   bool _aPublishHasFailed;

   ///<summary>Whether this taggregate has published, or been loaded with, a tevent that deletes it, an <see cref="ITaggregateDeletedTevent"/>.</summary>
   bool _isDeleted;

   ///<summary>The tevents of publish cascades that have ended, yet to be handed to the observers of <see cref="ITaggregate.TeventStream"/>,<br/>
   /// in the order published.</summary>
   readonly Queue<TWrapperTeventInterface> _teventsToHandOut = new();

   ///<summary>Whether tevents are being handed to the observers of <see cref="ITaggregate.TeventStream"/>, so that a publish an observer<br/>
   /// makes meanwhile, on this taggregate, has its tevents handed out after those before them, not from inside the observer.</summary>
   bool _handingOutTevents;

   ///<summary>How many tevents the handlers that one hand-out reaches may publish on this taggregate before it gives up: two handlers<br/>
   /// that cause each other's tevents would otherwise hand out tevents without end.</summary>
   const int MaximumTeventsPublishedWhileHandingOut = 100;

   ///<summary>Throws unless this instance may be changed. A committed instance, which a taggregate store serves to readers, is read<br/>
   /// only: it is changed by getting it for update, inside a unit of work.</summary>
   ///<remarks><see cref="Publish{TTevent}"/> asserts it itself. Call it first where the class changes its state other than by<br/>
   /// applying a tevent, such as while its parts are being moved to tevents, so that a change to a committed instance fails before<br/>
   /// it is made.</remarks>
   protected void AssertCanBeChanged() =>
      State.Assert(!_isReadOnly, () => $"{GetType().Name} {Id} is an instance a taggregate store serves to readers, which is read only. Get it for update from an ITaggregateStoreUpdater, inside a unit of work, to change it.");

   protected TTevent Publish<TTevent>(TTevent tevent) where TTevent : TTaggregateTeventImplementation
   {
      AssertCanBeChanged();
      try
      {
         return PublishCore(tevent);
      }
      catch
      {
         _aPublishHasFailed = true;
         throw;
      }
   }

   ///<summary>Gives a new, empty instance, being restored from a snapshot of a taggregate's state, the taggregate's id. The<br/>
   /// restoring code calls it first, before it sets any other state. A restored taggregate exists, so its version is at least 1.<br/>
   /// From then until the restore is over, its entity collections take restored entities, through <c>AddRestored</c>. A restore by a<br/>
   /// taggregate store is over when the restoring code returns; any other, at the instance's first publish.</summary>
   protected void RestoreIdentity(TaggregateId id)
   {
      State.Assert(Version == 0, () => $"{nameof(RestoreIdentity)} restores a new, empty instance, and this {GetType().Name} has version {Version}.");
#pragma warning disable CS0618 // Type or member is obsolete
      base.Id = id;
#pragma warning restore CS0618 // Type or member is obsolete
      Version = 1;
      _isBeingRestored = true;
   }

   TTevent PublishCore<TTevent>(TTevent tevent) where TTevent : TTaggregateTeventImplementation
   {
      State.Assert(!_applyingTevents, () => "You cannot raise tevents from within tevent appliers");
      _isBeingRestored = false;
      State.Assert(!_isDeleted, () => $"{GetType().Name} {Id} is deleted: it has published an {nameof(ITaggregateDeletedTevent)}, so it can publish nothing more, such as {tevent.GetType().FullName}.");

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
         Apply(wrapped);
         _unCommittedTevents.Add(wrapped);
         _teventsToHandOut.Enqueue(wrapped);
         TeventHandlers?.Dispatch(this, wrapped);
      }

      if(_reentrancyLevel == 0)
      {
         AssertInvariants(); //It is allowed to enter a temporarily invalid state that will be corrected by new tevents published by tevent handlers. So we only check invariants once this tevent has been fully published including tevents published by handlers of the original tevent.
         HandOutTheTeventsUnlessHandingOutAlready();
      }

      return tevent;
   }

   ///<summary>Hands the tevents of the ended publish cascades to the observers of <see cref="ITaggregate.TeventStream"/>, one at a time, in<br/>
   /// the order published, until none is left. Called by a publish that an observer makes on this taggregate, it returns at once: the<br/>
   /// hand-out under way hands that publish's tevents out after those before them.</summary>
   ///<remarks>When an observer throws, the tevents not yet handed out stay queued, and go out before the next publish's.</remarks>
   void HandOutTheTeventsUnlessHandingOutAlready()
   {
      if(_handingOutTevents) return;

      var maximumToHandOut = _teventsToHandOut.Count + MaximumTeventsPublishedWhileHandingOut;
      using(ScopedChange.Enter(() => _handingOutTevents = true, () => _handingOutTevents = false))
         for(var handedOut = 1; _teventsToHandOut.TryDequeue(out var next); handedOut++)
         {
            State.Assert(handedOut <= maximumToHandOut,
                         () => $"While {GetType().Name} {Id} handed out its tevents, the handlers they reached published {MaximumTeventsPublishedWhileHandingOut} more on it, so handlers seem to cause each other's tevents without end. The last: {next.Tevent.GetType().FullName}.");
            _teventStream.OnNext(next);
         }
   }

   ///<summary>Applies <paramref name="wrapped"/>, as any root does, refusing a publish from inside an applier, and noting a deletion.</summary>
   private protected override void Apply(IPublisherTevent<TTaggregateTevent> wrapped)
   {
      using(ScopedChange.Enter(() => _applyingTevents = true, () => _applyingTevents = false))
      {
         if(wrapped.Tevent is ITaggregateDeletedTevent) _isDeleted = true;
         base.Apply(wrapped);
      }
   }

   private protected override void AssertInvariants()
   {
      Invariant.Assert(Id != null, Id.Value != Guid.Empty);
      base.AssertInvariants();
   }

   readonly SimpleObservable<ITaggregateTevent<TTaggregateTevent>> _teventStream = new();
#pragma warning disable CA1033 //These method should NOT clutter the public interface of Taggregates.
   void ITeventiveInternals<TTaggregateTevent, TTaggregateTeventImplementation>.PublishInternal(TTaggregateTeventImplementation theTevent) => Publish(theTevent);

   void ITaggregateStoreInternals.MakeReadOnly() => _isReadOnly = true;
   void ITaggregateStoreInternals.EndTheRestore() => _isBeingRestored = false;
   bool ITeventiveInATaggregate.ItsTaggregateIsBeingRestored => _isBeingRestored && !_isReadOnly;
   bool ITaggregateStoreInternals.APublishHasFailed => _aPublishHasFailed;
   void ITaggregateStoreInternals.ContinueVersion(int version) => Version = version;

   IObservable<ITaggregateTevent<ITaggregateTevent>> ITaggregate.TeventStream => _teventStream;
   IObservable<ITaggregateTevent<TTaggregateTevent>> ITaggregate<TTaggregateTevent>.TeventStream => _teventStream;

   void ITaggregate.Commit(Action<IReadOnlyList<ITaggregateTevent<ITaggregateTevent>>> commitTevents)
   {
      //Taken out before they're handed over, so that a tevent published by whatever they're handed to stays uncommitted, for a
      //later commit, instead of being forgotten with them.
      List<ITaggregateTevent<ITaggregateTevent>> tevents = [.._unCommittedTevents];
      _unCommittedTevents.Clear();
      commitTevents(tevents);
   }

   void ITaggregate.LoadFromHistory(IEnumerable<ITaggregateTevent<ITaggregateTevent>> history) => ApplyHistory(history);
#pragma warning restore CA1033
}
