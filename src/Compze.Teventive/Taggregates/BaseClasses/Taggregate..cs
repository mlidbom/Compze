using Compze.Abstractions;
using Compze.Abstractions.Time;
using Compze.Contracts;
using Compze.Internals.SystemCE.ReactiveCE;
using Compze.Tessaging.TessageTypes;
using Compze.Teventive._internal;
using Compze.Teventive._private.Implementation;
using Compze.Teventive.Taggregates.Tevents;

namespace Compze.Teventive.Taggregates.BaseClasses;

public partial class Taggregate<TTaggregate, TTaggregateTevent, TTaggregateTeventImplementation, TWrapperTeventInterface, TWrapperTeventImplementation> :
   VersionedEntity<TTaggregate>,
   ITaggregate<TTaggregateTevent>,
   ITeventiveInternals<TTaggregateTevent, TTaggregateTeventImplementation>,
   ITaggregateStoreInternals,
   ITeventRouted
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
      TeventRouting.AssertDeclaredForTheClassOf(this);
   }

#pragma warning disable CA1033 //A class that declares its tevent routing re-implements ITeventRouted; the base class's null means "registers per instance", which no subclass calls.
   TeventRouting? ITeventRouted.TeventAppliers => null;
   TeventRouting? ITeventRouted.TeventHandlers => null;
#pragma warning restore CA1033

   ///<summary>The routing of this taggregate's class to its tevent appliers, when the class declares one, read through <see cref="ITeventRouted"/>.</summary>
   TeventRouting? TeventAppliers => ((ITeventRouted)this).TeventAppliers;

   ///<summary>The routing of this taggregate's class to its tevent handlers, when the class declares one, read through <see cref="ITeventRouted"/>.</summary>
   TeventRouting? TeventHandlers => ((ITeventRouted)this).TeventHandlers;

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
   bool _isReadOnly;
   bool _aPublishHasFailed;

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
   /// restoring code calls it first, before it sets any other state. A restored taggregate exists, so its version is at least 1.</summary>
   protected void RestoreIdentity(TaggregateId id)
   {
      State.Assert(Version == 0, () => $"{nameof(RestoreIdentity)} restores a new, empty instance, and this {GetType().Name} has version {Version}.");
#pragma warning disable CS0618 // Type or member is obsolete
      base.Id = id;
#pragma warning restore CS0618 // Type or member is obsolete
      Version = 1;
   }

   TTevent PublishCore<TTevent>(TTevent tevent) where TTevent : TTaggregateTeventImplementation
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
         _teventsToHandOut.Enqueue(wrapped);
         TeventHandlers?.Dispatch(this, wrapped);
         _teventHandlersDispatcher?.Dispatch(wrapped);
      }

      if(_reentrancyLevel == 0)
      {
         AssertInvariantsAreMetInternal(); //It is allowed to enter a temporarily invalid state that will be corrected by new tevents published by tevent handlers. So we only check invariants once this tevent has been fully published including tevents published by handlers of the original tevent.
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

   void ITaggregateStoreInternals.MakeReadOnly() => _isReadOnly = true;
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

   void ITaggregate.LoadFromHistory(IEnumerable<ITaggregateTevent<ITaggregateTevent>> history)
   {
      State.Assert(Version == 0, () => $"You can only call {nameof(ITaggregate.LoadFromHistory)} on an empty Taggregate with {nameof(Version)} == 0");
      //The stored wrapper is applied as it stands: after a migration has rewritten history the stored wrapper is the truth, not what this taggregate would wrap today.
      history.ForEach(wrappedTevent => ApplyTevent((TTaggregateTevent)wrappedTevent.Tevent, (IPublisherTevent<TTaggregateTevent>)wrappedTevent));
      AssertInvariantsAreMetInternal();
   }
#pragma warning restore CA1033
}
