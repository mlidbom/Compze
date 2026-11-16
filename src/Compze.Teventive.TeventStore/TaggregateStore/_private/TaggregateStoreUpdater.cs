using Compze.Abstractions;
using System.Diagnostics.CodeAnalysis;
using System.Transactions;
using Compze.Contracts;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Internals.SystemCE.ReactiveCE;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Tessaging.TessageBus;
using Compze.Teventive._internal;
using Compze.Teventive.Taggregates.Tevents;
using Compze.Teventive.TeventStore.Abstractions._internal.Exceptions;
using static Compze.Contracts.Contract;

namespace Compze.Teventive.TaggregateStore._private;

///<summary>The <see cref="ITaggregateStoreUpdater"/> of one unit of work, and its participant in the unit of work's transaction.<br/>
/// It collects the tevents its copies publish, in the order published, and dispatches them when the transaction prepares, before<br/>
/// any participant has voted, so the participation handlers run inside the transaction. What they publish in turn is dispatched in<br/>
/// the same pass. When the transaction commits, its copies become the committed instances.</summary>
sealed class TaggregateStoreUpdater : VolatileTransactionParticipant, ITaggregateStoreUpdater, IDisposable
{
   readonly InMemoryTaggregateStore _store;
   readonly IUnitOfWorkTeventPublisher _teventPublisher;
   readonly Dictionary<TaggregateId, TrackedTaggregate> _trackedTaggregates = new();
   readonly Queue<ITaggregateTevent<ITaggregateTevent>> _teventsToDispatch = new();
   readonly List<SemaphoreSlim> _heldUpdateLocks = [];
   readonly List<IDisposable> _teventStreamSubscriptions = [];

   internal TaggregateStoreUpdater(InMemoryTaggregateStore store, IUnitOfWorkTeventPublisher teventPublisher) : base(EnlistmentOptions.EnlistDuringPrepareRequired)
   {
      _store = store;
      _teventPublisher = teventPublisher;
   }

   public TTaggregate Get<TTaggregate>(TaggregateId id) where TTaggregate : class, ISnapshotTaggregate =>
      TryGet(id, out TTaggregate? taggregate) ? taggregate : throw new TaggregateNotFoundException(id);

   public bool TryGet<TTaggregate>(TaggregateId id, [NotNullWhen(true)] out TTaggregate? taggregate) where TTaggregate : class, ISnapshotTaggregate
   {
      EnlistInTheUnitOfWork();
      if(_trackedTaggregates.TryGetValue(id, out var tracked))
      {
         State.Assert(!tracked.IsDeleted, () => $"Taggregate {id} is deleted in this unit of work.");
         taggregate = (TTaggregate)tracked.Taggregate;
         return true;
      }

      HoldUpdateLock(id);
      var committed = _store.CommittedInstanceOf(id);
      if(committed == null)
      {
         taggregate = null;
         return false;
      }

      taggregate = (TTaggregate)committed.TakeSnapshot().Restore(id, committed.Version);
      Track(taggregate);
      return true;
   }

   public void Save<TTaggregate>(TTaggregate taggregate) where TTaggregate : class, ISnapshotTaggregate
   {
      EnlistInTheUnitOfWork();
      State.Assert(taggregate.Version > 0, () => $"{taggregate.GetType().Name} has published no tevent, so it has no id: there is nothing to save.");
      State.Assert(!_trackedTaggregates.ContainsKey(taggregate.Id), () => $"Taggregate {taggregate.Id} is already in this unit of work.");
      HoldUpdateLock(taggregate.Id);
      State.Assert(_store.CommittedInstanceOf(taggregate.Id) == null, () => $"The store already holds a taggregate {taggregate.Id}.");

      //Saving publishes nothing: the tevents the new taggregate published before it was saved wait, with all others, for the commit.
      taggregate.Commit(tevents => tevents.ForEach(_teventsToDispatch.Enqueue));
      Track(taggregate);
   }

   public void Delete(TaggregateId id)
   {
      Get<ISnapshotTaggregate>(id);
      _trackedTaggregates[id].IsDeleted = true;
   }

   void Track(ISnapshotTaggregate taggregate)
   {
      _trackedTaggregates.Add(taggregate.Id, new TrackedTaggregate(taggregate));
      //Only collected here, never dispatched: nothing runs while a domain method of the taggregate is still on the stack.
      _teventStreamSubscriptions.Add(taggregate.TeventStream.Subscribe(_teventsToDispatch.Enqueue));
   }

   void HoldUpdateLock(TaggregateId id) => _heldUpdateLocks.Add(_store.TakeUpdateLock(id));

   void EnlistInTheUnitOfWork()
   {
      State.Assert(Transaction.Current != null, () => $"{nameof(ITaggregateStoreUpdater)} changes taggregates within a unit of work, and there is no ambient transaction. Run the work through ExecuteUnitOfWork.");
      EnsureEnlistedInAnyAmbientTransaction();
   }

   protected override void OnEnlist() =>
      //Completion comes after the commit or rollback notification, so the next unit of work to take a lock finds the committed instance.
      Transaction.Current!.TransactionCompleted += (_, _) => ReleaseUpdateLocks();

   protected override void OnPrepare()
   {
      AssertNoPublishHasFailed();
      while(_teventsToDispatch.TryDequeue(out var tevent))
      {
         //Taggregate tevents are exactly-once kinds today, and the publisher accepts those only asynchronously.
         _teventPublisher.PublishAsync(tevent).GetAwaiter().GetResult();
      }
      AssertNoPublishHasFailed();
   }

   void AssertNoPublishHasFailed() =>
      State.Assert(_trackedTaggregates.Values.None(it => it.APublishHasFailed),
                   () => $"A publish failed on {_trackedTaggregates.Values.First(it => it.APublishHasFailed).Taggregate.Id} in this unit of work, so its state cannot be trusted. The unit of work rolls back.");

   protected override void OnCommit()
   {
      foreach(var tracked in _trackedTaggregates.Values)
      {
         if(tracked.IsDeleted)
         {
            _store.Remove(tracked.Taggregate.Id);
         } else
         {
            tracked.Taggregate.Commit(_ => {}); //Its tevents were dispatched from its tevent stream: this only clears them.
            _store.MakeCommitted(tracked.Taggregate);
         }
      }
   }

   protected override void OnRollback() {} //The copies, and the new taggregates, are simply never committed.

   void ReleaseUpdateLocks()
   {
      _heldUpdateLocks.ForEach(it => it.Release());
      _heldUpdateLocks.Clear();
   }

   public void Dispose() => _teventStreamSubscriptions.ForEach(it => it.Dispose());

   sealed class TrackedTaggregate(ISnapshotTaggregate taggregate)
   {
      public ISnapshotTaggregate Taggregate { get; } = taggregate;
      public bool IsDeleted { get; set; }
      public bool APublishHasFailed => ((ITaggregateStoreInternals)Taggregate).APublishHasFailed;
   }
}
