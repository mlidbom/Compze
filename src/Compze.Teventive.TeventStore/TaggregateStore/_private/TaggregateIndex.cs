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

   //Read without a lock: each is replaced whole, under the monitor, when a commit changes it.
   volatile ImmutableDictionary<TKey, ImmutableHashSet<TaggregateId>> _committedIdsByKey = ImmutableDictionary<TKey, ImmutableHashSet<TaggregateId>>.Empty;
   ImmutableDictionary<TaggregateId, ImmutableHashSet<TKey>> _committedKeysById = ImmutableDictionary<TaggregateId, ImmutableHashSet<TKey>>.Empty;

   public IReadOnlyList<ITaggregateReference<TTaggregate>> Lookup(TKey key)
   {
      var committedIds = _committedIdsByKey.GetValueOrDefault(key) ?? ImmutableHashSet<TaggregateId>.Empty;
      var session = store.AmbientSession;
      if(session is not { HoldsAnyTaggregate: true })
         return [..committedIds.Select(store.Reference<TTaggregate>)];

      //Read your own writes: what the unit of work holds is filed as its copies now are, not as they were committed.
      var idsAsTheUnitOfWorkLeftThem = committedIds.Where(id => !session.Holds(id))
                                                   .Concat(session.HeldInstancesOf<TTaggregate>().Where(it => keysOf(it).Contains(key)).Select(it => it.Id));
      return [..idsAsTheUnitOfWorkLeftThem.Select(store.Reference<TTaggregate>)];
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
      var idsByKey = _committedIdsByKey.ToBuilder();
      var keysById = _committedKeysById.ToBuilder();
      foreach(var (id, newKeys) in newKeysById)
      {
         var oldKeys = keysById.GetValueOrDefault(id) ?? [];
         foreach(var removedKey in oldKeys.Except(newKeys))
         {
            var remaining = idsByKey[removedKey].Remove(id);
            if(remaining.IsEmpty) idsByKey.Remove(removedKey);
            else idsByKey[removedKey] = remaining;
         }

         foreach(var addedKey in newKeys.Except(oldKeys))
            idsByKey[addedKey] = (idsByKey.GetValueOrDefault(addedKey) ?? []).Add(id);

         if(newKeys.IsEmpty) keysById.Remove(id);
         else keysById[id] = newKeys;
      }

      _committedKeysById = keysById.ToImmutable();
      _committedIdsByKey = idsByKey.ToImmutable();
   });

   sealed class PreparedChange(TaggregateIndex<TTaggregate, TKey> index, IReadOnlyList<(TaggregateId Id, ImmutableHashSet<TKey> NewKeys)> newKeysById) : IPreparedIndexChange
   {
      public void Apply() => index.Apply(newKeysById);
   }
}
