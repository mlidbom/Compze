using System.Diagnostics.CodeAnalysis;
using Compze.Abstractions;
using Compze.Contracts;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Internals.SystemCE.ReactiveCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Teventive._internal;
using Compze.Teventive.MemoryImageStateStore._private.Storage;
using Compze.Teventive.TaggregateStores._internal;
using Compze.Teventive.Taggregates.Tevents;
using static Compze.Contracts.Contract;

namespace Compze.Teventive.MemoryImageStateStore._private;

///<summary>The memory image state store's part of one unit of work: the private copies of the taggregates it got for update, the new ones it<br/>
/// added, and the locks of all of them. The tevents the copies publish go to the unit of work, which dispatches them as each<br/>
/// publish cascade ends, inside the transaction. A copy that publishes an <see cref="ITaggregateDeletedTevent"/> is deleted: the<br/>
/// unit of work no longer finds it, and the commit takes it out of the store. When the transaction prepares, this session prepares the storage's<br/>
/// write and the indexes' changes, and at the commit it makes the copies the committed instances.</summary>
sealed class MemoryImageStateStoreSession : ITaggregateStoreSession
{
   readonly TaggregateMemoryImageStateStore _store;
   readonly TaggregateUnitOfWork _unitOfWork;
   readonly Dictionary<TaggregateId, HeldTaggregate> _heldTaggregates = new();
   readonly HashSet<Type> _classesHeld = [];
   readonly Dictionary<TaggregateId, TaggregateUpdateLocks.UpdateLock> _heldUpdateLocks = new();
   readonly List<IDisposable> _teventStreamSubscriptions = [];
   readonly Action _leaveTheGate;
   IPreparedWrite? _preparedWrite;
   IReadOnlyList<IPreparedIndexChange> _preparedIndexChanges = [];

   ///<summary>The session of <paramref name="unitOfWork"/> in <paramref name="store"/>. <paramref name="leaveTheGate"/> runs when the<br/>
   /// unit of work completes, so that a reload waiting for it may begin.</summary>
   internal MemoryImageStateStoreSession(TaggregateMemoryImageStateStore store, TaggregateUnitOfWork unitOfWork, Action leaveTheGate)
   {
      _store = store;
      _unitOfWork = unitOfWork;
      _leaveTheGate = leaveTheGate;
   }

   ///<summary>Whether this unit of work holds a copy of any <typeparamref name="TTaggregate"/>, deleted or not. Allocates nothing.</summary>
   internal bool HoldsAny<TTaggregate>()
   {
      foreach(var heldClass in _classesHeld)
         if(heldClass.IsAssignableTo(typeof(TTaggregate))) return true;
      return false;
   }

   internal bool Holds(TaggregateId id) => _heldTaggregates.ContainsKey(id);

   ///<summary>Whether this unit of work holds the taggregate <paramref name="id"/>, and if so its copy, or null when the copy is deleted.</summary>
   internal bool TryGetHeld(TaggregateId id, out ITaggregate? held)
   {
      var holds = _heldTaggregates.TryGetValue(id, out var heldTaggregate);
      held = holds && !heldTaggregate!.IsDeleted ? heldTaggregate.Taggregate : null;
      return holds;
   }

   ///<summary>The copies and new taggregates of <typeparamref name="TTaggregate"/> this unit of work holds, deleted ones not included.</summary>
   internal IEnumerable<TTaggregate> HeldInstancesOf<TTaggregate>() where TTaggregate : class, ITaggregate =>
      _heldTaggregates.Values.Where(it => !it.IsDeleted).Select(it => it.Taggregate).OfType<TTaggregate>();

   public bool TryGetForUpdate<TTaggregate>(TaggregateId id, [NotNullWhen(true)] out TTaggregate? taggregate) where TTaggregate : class, ITaggregate
   {
      if(_heldTaggregates.TryGetValue(id, out var held))
      {
         taggregate = held.IsDeleted ? null : AsRequested<TTaggregate>(held.Taggregate);
         return taggregate != null;
      }

      HoldUpdateLock(id); //Held also when the store holds no such taggregate, so no other unit of work adds one before this one completes.
      taggregate = HoldACopyOfTheCommitted<TTaggregate>(id);
      return taggregate != null;
   }

   public async Task<TTaggregate?> TryGetForUpdateAsync<TTaggregate>(TaggregateId id) where TTaggregate : class, ITaggregate
   {
      if(_heldTaggregates.TryGetValue(id, out var held)) return held.IsDeleted ? null : AsRequested<TTaggregate>(held.Taggregate);

      await HoldUpdateLockAsync(id).caf(); //Held also when the store holds no such taggregate, as in TryGetForUpdate.
      return HoldACopyOfTheCommitted<TTaggregate>(id);
   }

   ///<summary>A copy of the committed taggregate <paramref name="id"/>, which this unit of work now holds, or null if the store holds no<br/>
   /// such taggregate. This unit of work holds its update lock already.</summary>
   TTaggregate? HoldACopyOfTheCommitted<TTaggregate>(TaggregateId id) where TTaggregate : class, ITaggregate
   {
      var committed = _store.Committed.InstanceOf(id);
      if(committed == null) return null;

      var copy = _store.DeclaredClassOf(committed.GetType()).CopyOf(committed);
      Hold(new HeldTaggregate(copy, isNew: false));
      return AsRequested<TTaggregate>(copy);
   }

   public void Add(ITaggregate taggregate)
   {
      _store.DeclaredClassOf(taggregate.GetType());
      State.Assert(taggregate.Version > 0, () => $"{taggregate.GetType().Name} has published no tevent, so it has no id: there is nothing to add.");
      State.Assert(!_heldTaggregates.ContainsKey(taggregate.Id), () => $"Taggregate {taggregate.Id} is already in this unit of work.");
      HoldUpdateLock(taggregate.Id);
      State.Assert(_store.Committed.InstanceOf(taggregate.Id) == null, () => $"The memory image state store already holds a taggregate {taggregate.Id}.");
      var tevents = TakeTheUncommittedTeventsOf(taggregate);
      State.Assert(IsFromItsCreationOn(tevents),
                   () => $"{taggregate.GetType().Name} {taggregate.Id} doesn't have the tevents it published from its creation on, so adding it would dispatch none of them: an earlier add took them, as in a unit of work that then rolled back. Create it again.");

      //Held before the tevents it published before it was added are dispatched, so that a handler that gets it meanwhile gets this
      //instance. A tevent a handler publishes on it meanwhile stays uncommitted, and is dispatched after those, in the order published.
      var held = new HeldTaggregate(taggregate, isNew: true);
      Hold(held, followingItsTeventStream: false);
      for(; tevents.Count > 0; tevents = TakeTheUncommittedTeventsOf(taggregate)) tevents.ForEach(tevent => Dispatch(tevent, publishedBy: held));
      FollowTheTeventStreamOf(held);

      static bool IsFromItsCreationOn(IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> tevents) => tevents.Count > 0 && tevents[0].Tevent.TaggregateVersion == 1;
   }

   static IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> TakeTheUncommittedTeventsOf(ITaggregate taggregate)
   {
      IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> taken = [];
      taggregate.Commit(tevents => taken = tevents);
      return taken;
   }

   void Hold(HeldTaggregate held, bool followingItsTeventStream = true)
   {
      _heldTaggregates.Add(held.Taggregate.Id, held);
      _classesHeld.Add(held.Taggregate.GetType());
      if(followingItsTeventStream) FollowTheTeventStreamOf(held);
   }

   ///<summary>Dispatches each tevent <paramref name="held"/> publishes from now on, as its taggregate hands it out.</summary>
   void FollowTheTeventStreamOf(HeldTaggregate held) =>
      _teventStreamSubscriptions.Add(held.Taggregate.TeventStream.Subscribe(tevent => Dispatch(tevent, publishedBy: held)));

   void Dispatch(ITaggregateTevent<ITaggregateTevent> tevent, HeldTaggregate publishedBy)
   {
      publishedBy.RecordThatItPublished(tevent);
      _unitOfWork.Dispatch(tevent);
   }

   ///<summary>Takes the lock of the taggregate <paramref name="id"/>, unless this unit of work holds it already, as it does after trying<br/>
   /// to get for update a taggregate the store doesn't hold: a lock can't be taken twice, even by its holder.</summary>
   void HoldUpdateLock(TaggregateId id)
   {
      if(_heldUpdateLocks.ContainsKey(id)) return;
      _heldUpdateLocks.Add(id, _store.UpdateLocks.Take(id));
   }

   ///<summary>As <see cref="HoldUpdateLock"/>, holding no thread while another unit of work holds the lock.</summary>
   async Task HoldUpdateLockAsync(TaggregateId id)
   {
      if(_heldUpdateLocks.ContainsKey(id)) return;
      _heldUpdateLocks.Add(id, await _store.UpdateLocks.TakeAsync(id).caf());
   }

   static TTaggregate AsRequested<TTaggregate>(ITaggregate taggregate) where TTaggregate : class, ITaggregate
   {
      State.Assert(taggregate is TTaggregate, () => $"Taggregate {taggregate.Id} is a {taggregate.GetType().FullName}, not a {typeof(TTaggregate).FullName}.");
      return (TTaggregate)taggregate;
   }

   public void AssertNoPublishHasFailed()
   {
      var failed = _heldTaggregates.Values.FirstOrDefault(it => ((ITaggregateStoreInternals)it.Taggregate).APublishHasFailed);
      State.Assert(failed == null, () => $"A publish failed on {failed!.Taggregate.GetType().Name} {failed.Taggregate.Id} in this unit of work, so its state can't be trusted. The unit of work rolls back.");
   }

   public void Prepare()
   {
      var changed = _heldTaggregates.Values.Where(it => it.IsChanged).ToList();
      _preparedWrite = _store.Storage.Prepare([..changed.Where(it => !it.IsDeleted).Select(SnapshotOf)],
                                              [..changed.Where(it => it is { IsDeleted: true, IsNew: false }).Select(it => it.Taggregate.Id)]);
      var changedStates = changed.Select(it => (it.Taggregate.Id, NewState: it.IsDeleted ? null : it.Taggregate)).ToList();
      _preparedIndexChanges = [.._store.Indexes.Select(index => index.Prepare(changedStates))];

      TaggregateSnapshot SnapshotOf(HeldTaggregate held) =>
         new(held.Taggregate.GetType(), held.Taggregate.Id, _store.DeclaredClassOf(held.Taggregate.GetType()).TakeSnapshot(held.Taggregate));
   }

   ///<remarks>Called once the transaction has committed: every database in it has committed, as the transaction's decision, before<br/>
   /// any participant is told the outcome. So the copies become the committed instances only once the storage holds them.</remarks>
   public void Commit()
   {
      var write = _preparedWrite._assert().NotNull();
      write.Commit();
      var changed = _heldTaggregates.Values.Where(it => it is { IsChanged: true, WasAddedAndDeleted: false }).ToList();
      foreach(var held in changed.Where(it => !it.IsDeleted)) held.Taggregate.Commit(_ => {}); //Its tevents were dispatched from its tevent stream: this only forgets them.
      _store.Committed.InstallACommit([..changed.Select(it => (it.Taggregate.Id, NewState: it.IsDeleted ? null : it.Taggregate))], _preparedIndexChanges);
   }

   public void Rollback() => _preparedWrite?.Discard(); //The copies and the new taggregates are simply never committed.

   public void Complete()
   {
      _teventStreamSubscriptions.ForEach(it => it.Dispose());
      _heldUpdateLocks.Values.ForEach(it => it.Release());
      _heldUpdateLocks.Clear();
      _leaveTheGate();
   }

   ///<summary>A taggregate this unit of work holds: a copy got for update, or a new one added.</summary>
   sealed class HeldTaggregate
   {
      internal HeldTaggregate(ITaggregate taggregate, bool isNew)
      {
         Taggregate = taggregate;
         IsNew = isNew;
      }

      internal ITaggregate Taggregate { get; }
      internal bool IsNew { get; }

      ///<summary>Whether the taggregate has published an <see cref="ITaggregateDeletedTevent"/>, so that the commit takes it out of the store.</summary>
      internal bool IsDeleted { get; private set; }

      bool _hasPublishedAny;

      ///<summary>Records that the taggregate has published <paramref name="tevent"/>.</summary>
      internal void RecordThatItPublished(ITaggregateTevent<ITaggregateTevent> tevent)
      {
         _hasPublishedAny = true;
         if(tevent.Tevent is ITaggregateDeletedTevent) IsDeleted = true;
      }

      ///<summary>Whether committing this unit of work changes the store's taggregate. A copy that published nothing changes nothing,<br/>
      /// and the committed instance stays.</summary>
      internal bool IsChanged => IsNew || _hasPublishedAny;

      ///<summary>Whether this unit of work both added the taggregate and deleted it, so that it was never stored, and its commit stores nothing of it.</summary>
      internal bool WasAddedAndDeleted => IsNew && IsDeleted;
   }
}
