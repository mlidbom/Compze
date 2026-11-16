using System.Collections.Concurrent;
using System.Collections.Immutable;
using Compze.Abstractions;
using Compze.Threading;

namespace Compze.Teventive.TaggregateStore._private;

///<summary>What the session of a unit of work needs of every index: to compute, at prepare, how the unit of work's taggregates<br/>
/// are to be filed, which runs the consumer's function from state to keys and may fail, and to apply that at commit, which can't.</summary>
interface ITaggregateIndexMaintenance
{
   IPreparedIndexChange Prepare(IReadOnlyList<(TaggregateId Id, ISnapshotTaggregate? NewState)> changedTaggregates);
}

interface IPreparedIndexChange
{
   void Apply();
}

sealed class TaggregateIndex<TTaggregate, TKey>(InMemoryTaggregateStore store, Func<TTaggregate, IEnumerable<TKey>> keysOf)
   : ITaggregateIndex<TTaggregate, TKey>, ITaggregateIndexMaintenance
   where TTaggregate : class, ISnapshotTaggregate where TKey : notnull
{
   readonly IMonitor _monitor = IMonitor.New(LockTimeout.Seconds(30));

   //Read without a lock: a key's array is replaced whole when a commit changes it, never changed in place, so a lookup
   //outside a unit of work returns it as it is, allocating nothing.
   readonly ConcurrentDictionary<TKey, ITaggregateReference<TTaggregate>[]> _committedReferencesByKey = new();
   readonly Dictionary<TaggregateId, ImmutableHashSet<TKey>> _committedKeysById = new(); //Only under the monitor.

   public IReadOnlyList<ITaggregateReference<TTaggregate>> Lookup(TKey key)
   {
      if(!_committedReferencesByKey.TryGetValue(key, out var committed)) committed = [];
      if(store.AmbientSession is not {} session || !session.HoldsAnyCopyOf<TTaggregate>()) return committed;
      return AsTheUnitOfWorkLeftThem(committed, key, session);
   }

   ///<summary>Read your own writes: what the unit of work holds is filed as its copies now are, not as they were committed.<br/>
   /// A method of its own, since its lambdas' closure would otherwise be allocated on every lookup, overlay or not.</summary>
   ITaggregateReference<TTaggregate>[] AsTheUnitOfWorkLeftThem(ITaggregateReference<TTaggregate>[] committed, TKey key, TaggregateStoreUpdater session) =>
      [..committed.Where(it => !session.Holds(it.Id)),
       ..session.HeldInstancesOf<TTaggregate>().Where(it => keysOf(it).Contains(key)).Select(it => store.Reference<TTaggregate>(it.Id))];

   public bool HasAny(TKey key)
   {
      if(store.AmbientSession is {} session && session.HoldsAnyCopyOf<TTaggregate>()) return Lookup(key).Count > 0;
      return _committedReferencesByKey.TryGetValue(key, out var committed) && committed.Length > 0;
   }

   public IPreparedIndexChange Prepare(IReadOnlyList<(TaggregateId Id, ISnapshotTaggregate? NewState)> changedTaggregates)
   {
      var newKeysById = changedTaggregates.Where(it => it.NewState is null or TTaggregate)
                                          .Select(it => (it.Id, NewKeys: it.NewState is TTaggregate taggregate ? keysOf(taggregate).ToImmutableHashSet() : []))
                                          .ToList();
      return new PreparedChange(this, newKeysById);
   }

   void Apply(IReadOnlyList<(TaggregateId Id, ImmutableHashSet<TKey> NewKeys)> newKeysById) => _monitor.Locked(() =>
   {
      foreach(var (id, newKeys) in newKeysById)
      {
         var oldKeys = _committedKeysById.GetValueOrDefault(id) ?? [];
         foreach(var removedKey in oldKeys.Except(newKeys))
         {
            ITaggregateReference<TTaggregate>[] remaining = [.._committedReferencesByKey[removedKey].Where(it => it.Id != id)];
            if(remaining.Length == 0) _committedReferencesByKey.TryRemove(removedKey, out _);
            else _committedReferencesByKey[removedKey] = remaining;
         }

         foreach(var addedKey in newKeys.Except(oldKeys))
            _committedReferencesByKey[addedKey] = [..(_committedReferencesByKey.GetValueOrDefault(addedKey) ?? []), store.Reference<TTaggregate>(id)];

         if(newKeys.IsEmpty) _committedKeysById.Remove(id);
         else _committedKeysById[id] = newKeys;
      }
   });

   sealed class PreparedChange(TaggregateIndex<TTaggregate, TKey> index, IReadOnlyList<(TaggregateId Id, ImmutableHashSet<TKey> NewKeys)> newKeysById) : IPreparedIndexChange
   {
      public void Apply() => index.Apply(newKeysById);
   }
}
