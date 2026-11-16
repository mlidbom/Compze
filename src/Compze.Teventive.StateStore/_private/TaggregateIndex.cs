using System.Collections.Concurrent;
using System.Collections.Immutable;
using Compze.Abstractions;
using Compze.Teventive.TaggregateStores;
using Compze.Threading.ResourceAccess;

namespace Compze.Teventive.StateStore._private;

///<summary>What the state store needs of each of its indexes: to file what it loads, to compute, when a unit of work prepares, how<br/>
/// the unit of work's taggregates are to be filed, which runs the consumer's function from state to keys and so may fail, and to<br/>
/// apply that when the unit of work commits, which can't fail.</summary>
interface ITaggregateIndexMaintenance
{
   ///<summary>Files exactly <paramref name="committedTaggregates"/>, and nothing filed before: when the store loads everything.</summary>
   void FileExactly(IEnumerable<ITaggregate> committedTaggregates);
   IPreparedIndexChange Prepare(IReadOnlyList<(TaggregateId Id, ITaggregate? NewState)> changedTaggregates);
}

///<summary>How an index files the taggregates a unit of work changed, computed when it prepared, and applied when it commits.</summary>
interface IPreparedIndexChange
{
   void Apply();
}

sealed class TaggregateIndex<TTaggregate, TKey> : ITaggregateIndex<TTaggregate, TKey>, ITaggregateIndexMaintenance
   where TTaggregate : class, ITaggregate where TKey : notnull
{
   readonly TaggregateStateStore _store;
   readonly Func<TTaggregate, IEnumerable<TKey>> _keysOf;

   //Read without a lock: a key's array is replaced whole when a commit changes it, never changed in place, so a lookup outside a
   //unit of work returns it as it is, allocating nothing.
   volatile ConcurrentDictionary<TKey, ITaggregateReference<TTaggregate>[]> _committedReferencesByKey = new(); //Replaced whole when the store loads everything.
   readonly IThreadShared<Dictionary<TaggregateId, ImmutableHashSet<TKey>>> _committedKeysById = IThreadShared.New(new Dictionary<TaggregateId, ImmutableHashSet<TKey>>());

   internal TaggregateIndex(TaggregateStateStore store, Func<TTaggregate, IEnumerable<TKey>> keysOf)
   {
      _store = store;
      _keysOf = keysOf;
   }

   public IReadOnlyCollection<ITaggregateReference<TTaggregate>> Lookup(TKey key)
   {
      _store.EnsureLoaded();
      if(!_committedReferencesByKey.TryGetValue(key, out var committed)) committed = [];
      if(_store.AmbientSession is not {} session || !session.HoldsAny<TTaggregate>()) return committed;
      return AsTheUnitOfWorkLeftThem(committed, key, session);
   }

   ///<summary>Read your own writes: what the unit of work holds is filed as its copies now are, not as they were committed.<br/>
   /// A method of its own, since its lambdas' closure would otherwise be allocated on every lookup, overlay or not.</summary>
   ///<remarks>performance: Recomputes the keys of every copy the unit of work holds at each lookup, so a lookup costs in proportion<br/>
   /// to the copies held: 54 to 58 µs with 1,000. Keep each copy's keys, recomputed only when it publishes (the-taggregate-store.md).</remarks>
   ITaggregateReference<TTaggregate>[] AsTheUnitOfWorkLeftThem(ITaggregateReference<TTaggregate>[] committed, TKey key, StateStoreSession session) =>
      [..committed.Where(it => !session.Holds(it.Id)),
       ..session.HeldInstancesOf<TTaggregate>().Where(it => _keysOf(it).Contains(key)).Select(it => _store.Reference<TTaggregate>(it.Id))];

   public bool HasAny(TKey key)
   {
      _store.EnsureLoaded();
      if(_store.AmbientSession is {} session && session.HoldsAny<TTaggregate>()) return Lookup(key).Count > 0;
      return _committedReferencesByKey.TryGetValue(key, out var committed) && committed.Length > 0;
   }

   public void FileExactly(IEnumerable<ITaggregate> committedTaggregates)
   {
      var keysById = committedTaggregates.OfType<TTaggregate>().Select(it => (it.Id, Keys: _keysOf(it).ToImmutableHashSet())).ToList();
      var referencesByKey = new ConcurrentDictionary<TKey, ITaggregateReference<TTaggregate>[]>(
         keysById.SelectMany(it => it.Keys.Select(key => (Key: key, it.Id)))
                 .GroupBy(it => it.Key, it => _store.Reference<TTaggregate>(it.Id))
                 .Select(group => KeyValuePair.Create(group.Key, group.ToArray())));
      _committedKeysById.Locked(committedKeysById =>
      {
         committedKeysById.Clear();
         foreach(var (id, keys) in keysById.Where(it => !it.Keys.IsEmpty)) committedKeysById[id] = keys;
         _committedReferencesByKey = referencesByKey;
      });
   }

   public IPreparedIndexChange Prepare(IReadOnlyList<(TaggregateId Id, ITaggregate? NewState)> changedTaggregates)
   {
      var newKeysById = changedTaggregates.Where(it => it.NewState is null or TTaggregate)
                                          .Select(it => (it.Id, NewKeys: it.NewState is TTaggregate taggregate ? _keysOf(taggregate).ToImmutableHashSet() : []))
                                          .ToList();
      return new PreparedChange(this, newKeysById);
   }

   void Apply(IReadOnlyList<(TaggregateId Id, ImmutableHashSet<TKey> NewKeys)> newKeysById) => _committedKeysById.Locked(committedKeysById =>
   {
      foreach(var (id, newKeys) in newKeysById)
      {
         var oldKeys = committedKeysById.GetValueOrDefault(id) ?? [];
         foreach(var removedKey in oldKeys.Except(newKeys))
         {
            ITaggregateReference<TTaggregate>[] remaining = [.._committedReferencesByKey[removedKey].Where(it => it.Id != id)];
            if(remaining.Length == 0) _committedReferencesByKey.TryRemove(removedKey, out _);
            else _committedReferencesByKey[removedKey] = remaining;
         }

         foreach(var addedKey in newKeys.Except(oldKeys))
            _committedReferencesByKey[addedKey] = [..(_committedReferencesByKey.GetValueOrDefault(addedKey) ?? []), _store.Reference<TTaggregate>(id)];

         if(newKeys.IsEmpty) committedKeysById.Remove(id);
         else committedKeysById[id] = newKeys;
      }
   });

   sealed class PreparedChange : IPreparedIndexChange
   {
      readonly TaggregateIndex<TTaggregate, TKey> _index;
      readonly IReadOnlyList<(TaggregateId Id, ImmutableHashSet<TKey> NewKeys)> _newKeysById;

      internal PreparedChange(TaggregateIndex<TTaggregate, TKey> index, IReadOnlyList<(TaggregateId Id, ImmutableHashSet<TKey> NewKeys)> newKeysById)
      {
         _index = index;
         _newKeysById = newKeysById;
      }

      public void Apply() => _index.Apply(_newKeysById);
   }
}
