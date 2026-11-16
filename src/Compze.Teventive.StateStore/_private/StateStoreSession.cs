using System.Diagnostics.CodeAnalysis;
using Compze.Abstractions;
using Compze.Contracts;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Internals.SystemCE.ReactiveCE;
using Compze.Teventive._internal;
using Compze.Teventive.StateStore.Storage;
using Compze.Teventive.TaggregateStores._internal;
using static Compze.Contracts.Contract;

namespace Compze.Teventive.StateStore._private;

///<summary>The state store's part of one unit of work: the private copies of the taggregates it got for update, the new ones it<br/>
/// saved, the ones it deleted, and the locks of all of them. The tevents the copies publish go to the unit of work, which publishes<br/>
/// them to be dispatched when its transaction prepares. Then this session prepares the storage's write and the indexes' changes, and<br/>
/// at the commit it makes the copies the committed instances.</summary>
sealed class StateStoreSession : ITaggregateStoreSession
{
   readonly TaggregateStateStore _store;
   readonly TaggregateUnitOfWork _unitOfWork;
   readonly Dictionary<TaggregateId, HeldTaggregate> _heldTaggregates = new();
   readonly HashSet<Type> _classesHeld = [];
   readonly List<TaggregateUpdateLocks.UpdateLock> _heldUpdateLocks = [];
   readonly List<IDisposable> _teventStreamSubscriptions = [];
   readonly Action _leaveTheGate;
   IPreparedWrite? _preparedWrite;
   IReadOnlyList<IPreparedIndexChange> _preparedIndexChanges = [];

   ///<summary>The session of <paramref name="unitOfWork"/> in <paramref name="store"/>. <paramref name="leaveTheGate"/> runs when the<br/>
   /// unit of work completes, so that a reload waiting for it may begin.</summary>
   internal StateStoreSession(TaggregateStateStore store, TaggregateUnitOfWork unitOfWork, Action leaveTheGate)
   {
      _store = store;
      _unitOfWork = unitOfWork;
      _leaveTheGate = leaveTheGate;
   }

   ///<summary>Whether this unit of work holds a copy or a deletion of any <typeparamref name="TTaggregate"/>. Allocates nothing.</summary>
   internal bool HoldsAny<TTaggregate>()
   {
      foreach(var heldClass in _classesHeld)
         if(heldClass.IsAssignableTo(typeof(TTaggregate))) return true;
      return false;
   }

   internal bool Holds(TaggregateId id) => _heldTaggregates.ContainsKey(id);

   ///<summary>Whether this unit of work holds the taggregate <paramref name="id"/>, and if so its copy, or null when it deleted it.</summary>
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
         State.Assert(!held.IsDeleted, () => $"Taggregate {id} is deleted in this unit of work.");
         taggregate = AsRequested<TTaggregate>(held.Taggregate);
         return true;
      }

      HoldUpdateLock(id); //Held also when the store holds no such taggregate, so no other unit of work adds one before this one completes.
      var committed = _store.CommittedInstanceOf(id);
      if(committed == null)
      {
         taggregate = null;
         return false;
      }

      var copy = _store.DeclaredClassOf(committed.GetType()).CopyOf(committed);
      Hold(new HeldTaggregate(copy, isNew: false));
      taggregate = AsRequested<TTaggregate>(copy);
      return true;
   }

   public void Save(ITaggregate taggregate)
   {
      _store.DeclaredClassOf(taggregate.GetType());
      State.Assert(taggregate.Version > 0, () => $"{taggregate.GetType().Name} has published no tevent, so it has no id: there is nothing to save.");
      State.Assert(!_heldTaggregates.ContainsKey(taggregate.Id), () => $"Taggregate {taggregate.Id} is already in this unit of work.");
      HoldUpdateLock(taggregate.Id);
      State.Assert(_store.CommittedInstanceOf(taggregate.Id) == null, () => $"The state store already holds a taggregate {taggregate.Id}.");

      //Saving dispatches nothing: the tevents the new taggregate published before it was saved wait, with all others, for the commit.
      taggregate.Commit(tevents => tevents.ForEach(_unitOfWork.Publish));
      Hold(new HeldTaggregate(taggregate, isNew: true));
   }

   public void Delete<TTaggregate>(TaggregateId id) where TTaggregate : class, ITaggregate
   {
      State.Assert(TryGetForUpdate(id, out TTaggregate? _), () => $"There is no taggregate {id} to delete.");
      _heldTaggregates[id].IsDeleted = true;
   }

   void Hold(HeldTaggregate held)
   {
      _heldTaggregates.Add(held.Taggregate.Id, held);
      _classesHeld.Add(held.Taggregate.GetType());
      //Only queued for the commit here, never dispatched: nothing runs while a domain method of the taggregate is still on the stack.
      _teventStreamSubscriptions.Add(held.Taggregate.TeventStream.Subscribe(tevent =>
      {
         held.HasPublished = true;
         _unitOfWork.Publish(tevent);
      }));
   }

   void HoldUpdateLock(TaggregateId id) => _heldUpdateLocks.Add(_store.UpdateLocks.Take(id));

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
                                              [..changed.Where(it => it is { IsDeleted: true, IsNew: false }).Select(it => new TaggregateToDelete(it.Taggregate.GetType(), it.Taggregate.Id))]);
      var changedStates = changed.Select(it => (it.Taggregate.Id, NewState: it.IsDeleted ? null : it.Taggregate)).ToList();
      _preparedIndexChanges = [.._store.Indexes.Select(index => index.Prepare(changedStates))];

      TaggregateSnapshot SnapshotOf(HeldTaggregate held) =>
         new(held.Taggregate.GetType(), held.Taggregate.Id, _store.DeclaredClassOf(held.Taggregate.GetType()).TakeSnapshot(held.Taggregate));
   }

   public void Commit()
   {
      _preparedWrite._assert().NotNull().Commit();
      foreach(var held in _heldTaggregates.Values.Where(it => it.IsChanged))
      {
         if(held.IsDeleted)
         {
            _store.Remove(held.Taggregate.Id);
         } else
         {
            held.Taggregate.Commit(_ => {}); //Its tevents were collected from its tevent stream: this only forgets them.
            _store.MakeCommitted(held.Taggregate);
         }
      }

      _preparedIndexChanges.ForEach(it => it.Apply());
   }

   public void Rollback() => _preparedWrite?.Discard(); //The copies and the new taggregates are simply never committed.

   public void Complete()
   {
      _teventStreamSubscriptions.ForEach(it => it.Dispose());
      _heldUpdateLocks.ForEach(it => it.Release());
      _heldUpdateLocks.Clear();
      _leaveTheGate();
   }

   ///<summary>A taggregate this unit of work holds: a copy got for update, or a new one saved.</summary>
   sealed class HeldTaggregate
   {
      internal HeldTaggregate(ITaggregate taggregate, bool isNew)
      {
         Taggregate = taggregate;
         IsNew = isNew;
      }

      internal ITaggregate Taggregate { get; }
      internal bool IsNew { get; }
      internal bool IsDeleted { get; set; }
      internal bool HasPublished { get; set; }

      ///<summary>Whether committing this unit of work changes the store's taggregate. A copy that published nothing changes nothing,<br/>
      /// and the committed instance stays.</summary>
      internal bool IsChanged => IsNew || IsDeleted || HasPublished;
   }
}
