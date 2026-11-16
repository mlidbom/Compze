using Compze.Abstractions;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Compze.Teventive._internal;
using Compze.Teventive.TeventStore.Abstractions._internal.Exceptions;

namespace Compze.Teventive.TaggregateStore._private;

///<summary>The committed taggregates of an in-memory taggregate store, and the lock each one is updated under.</summary>
sealed class InMemoryTaggregateStore : ITaggregateStoreReader
{
   static readonly TimeSpan UpdateLockTimeout = TimeSpan.FromSeconds(30);

   readonly ConcurrentDictionary<TaggregateId, ISnapshotTaggregate> _committedTaggregates = new();
   readonly ConcurrentDictionary<TaggregateId, SemaphoreSlim> _updateLocks = new();

   public TTaggregate Get<TTaggregate>(TaggregateId id) where TTaggregate : class, ISnapshotTaggregate =>
      TryGet(id, out TTaggregate? taggregate) ? taggregate : throw new TaggregateNotFoundException(id);

   public bool TryGet<TTaggregate>(TaggregateId id, [NotNullWhen(true)] out TTaggregate? taggregate) where TTaggregate : class, ISnapshotTaggregate
   {
      taggregate = CommittedInstanceOf(id) as TTaggregate;
      return taggregate != null;
   }

   internal ISnapshotTaggregate? CommittedInstanceOf(TaggregateId id) => _committedTaggregates.GetValueOrDefault(id);

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
