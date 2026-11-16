using Compze.Abstractions;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Transactions;
using Compze.Teventive._internal;
using Compze.Teventive.TeventStore.Abstractions._internal.Exceptions;

namespace Compze.Teventive.TaggregateStore._private;

///<summary>The committed taggregates of an in-memory taggregate store, the lock each one is updated under, its indexes, and the<br/>
/// session of each transaction that is changing taggregates, so that references and indexes find the ambient one.</summary>
sealed class InMemoryTaggregateStore : ITaggregateStoreReader
{
   static readonly TimeSpan UpdateLockTimeout = TimeSpan.FromSeconds(30);

   readonly ConcurrentDictionary<TaggregateId, ISnapshotTaggregate> _committedTaggregates = new();
   readonly ConcurrentDictionary<TaggregateId, SemaphoreSlim> _updateLocks = new();
   readonly ConcurrentDictionary<string, TaggregateStoreUpdater> _sessionsByTransaction = new();
   readonly List<ITaggregateIndexMaintenance> _indexes = [];
   readonly ConcurrentDictionary<TaggregateId, object> _references = new();

   public TTaggregate Get<TTaggregate>(TaggregateId id) where TTaggregate : class, ISnapshotTaggregate =>
      TryGet(id, out TTaggregate? taggregate) ? taggregate : throw new TaggregateNotFoundException(id);

   public bool TryGet<TTaggregate>(TaggregateId id, [NotNullWhen(true)] out TTaggregate? taggregate) where TTaggregate : class, ISnapshotTaggregate
   {
      taggregate = CommittedInstanceOf(id) as TTaggregate;
      return taggregate != null;
   }

   ///<summary>One reference for each taggregate, made at the first request, so that lookups allocate none.</summary>
   public ITaggregateReference<TTaggregate> Reference<TTaggregate>(TaggregateId id) where TTaggregate : class, ISnapshotTaggregate =>
      _references.GetOrAdd(id, static (theId, store) => new TaggregateReference<TTaggregate>(store, theId), this) as ITaggregateReference<TTaggregate>
   ?? new TaggregateReference<TTaggregate>(this, id); //First asked for as another type: a reference is a value, compared by id.

   internal ISnapshotTaggregate? CommittedInstanceOf(TaggregateId id) => _committedTaggregates.GetValueOrDefault(id);

   ///<summary>The session of the ambient transaction, if one of this store's sessions is enlisted in it.</summary>
   internal TaggregateStoreUpdater? AmbientSession =>
      Transaction.Current is {} ambient ? _sessionsByTransaction.GetValueOrDefault(ambient.TransactionInformation.LocalIdentifier) : null;

   internal void RegisterSession(Transaction transaction, TaggregateStoreUpdater session) =>
      _sessionsByTransaction[transaction.TransactionInformation.LocalIdentifier] = session;

   internal void UnregisterSession(Transaction transaction) =>
      _sessionsByTransaction.TryRemove(transaction.TransactionInformation.LocalIdentifier, out _);

   internal void AddIndex(ITaggregateIndexMaintenance index) => _indexes.Add(index);

   internal IReadOnlyList<ITaggregateIndexMaintenance> Indexes => _indexes;

   ///<summary>Takes the lock that one unit of work at a time holds while it changes the taggregate <paramref name="id"/>. It is<br/>
   /// released when that unit of work completes, possibly on another thread, so it is a <see cref="SemaphoreSlim"/>.</summary>
   internal SemaphoreSlim TakeUpdateLock(TaggregateId id)
   {
      var updateLock = _updateLocks.GetOrAdd(id, static _ => new SemaphoreSlim(1, 1));
      if(!updateLock.Wait(UpdateLockTimeout))
         throw new TimeoutException($"Waited {UpdateLockTimeout} for another unit of work to finish changing taggregate {id}.");
      return updateLock;
   }

   internal void MakeCommitted(ISnapshotTaggregate taggregate)
   {
      ((ITaggregateStoreInternals)taggregate).MakeReadOnly();
      _committedTaggregates[taggregate.Id] = taggregate;
   }

   internal void Remove(TaggregateId id) => _committedTaggregates.TryRemove(id, out _);
}
