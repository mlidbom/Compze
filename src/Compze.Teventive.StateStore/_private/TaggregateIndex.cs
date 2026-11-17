using System.Collections.Concurrent;
using Compze.Abstractions;
using Compze.Contracts;
using Compze.Teventive.TaggregateStores;
using Compze.Threading;
using static Compze.Contracts.Contract;

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
   readonly IMonitor _filingMonitor = IMonitor.New(); //Serializes the commits' changes to the arrays, each of which reads a key's array and replaces it.

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
      var referencesByKey = new ConcurrentDictionary<TKey, ITaggregateReference<TTaggregate>[]>(
         committedTaggregates.OfType<TTaggregate>()
                             .SelectMany(taggregate => KeysOf(taggregate).Select(key => (Key: key, taggregate.Id)))
                             .GroupBy(it => it.Key, it => _store.Reference<TTaggregate>(it.Id))
                             .Select(group => KeyValuePair.Create(group.Key, group.ToArray())));
      _filingMonitor.Locked(() => _committedReferencesByKey = referencesByKey);
   }

   ///<summary>The keys each changed taggregate leaves and gains. The index keeps no record of a taggregate's keys: those it leaves are<br/>
   /// the keys of its committed instance, which stays the old one until the commit, and which no other unit of work can commit<br/>
   /// meanwhile, since this one holds the taggregate's lock.</summary>
   public IPreparedIndexChange Prepare(IReadOnlyList<(TaggregateId Id, ITaggregate? NewState)> changedTaggregates)
   {
      var refilings = changedTaggregates.Select(it => new Refiling(it.Id,
                                                                   oldKeys: KeysOf(_store.CommittedInstanceOf(it.Id) as TTaggregate),
                                                                   newKeys: KeysOf(it.NewState as TTaggregate)))
                                        .Where(it => it.ChangesTheIndex)
                                        .ToList();
      refilings.ForEach(AssertFiledUnderItsOldKeys);
      return new PreparedChange(this, refilings);
   }

   ///<summary>The keys <see cref="_keysOf"/> gives <paramref name="taggregate"/>, each once; none for no taggregate.</summary>
   TKey[] KeysOf(TTaggregate? taggregate) => taggregate == null ? [] : [.._keysOf(taggregate).Distinct()];

   ///<summary>Fails the unit of work, while it can still roll back, if the key function gives the committed instance a key it isn't<br/>
   /// filed under: a key function that reads anything but the taggregate's own state, which the committed index can't follow.</summary>
   void AssertFiledUnderItsOldKeys(Refiling refiling)
   {
      foreach(var oldKey in refiling.OldKeys)
         State.Assert(_committedReferencesByKey.TryGetValue(oldKey, out var filed) && filed.Any(it => it.Id == refiling.Id),
                      () => $"The index of {typeof(TTaggregate).FullName} by {typeof(TKey).FullName} gives taggregate {refiling.Id}'s committed state the key {oldKey}, but doesn't file it there. An index's key function must give keys from the taggregate's own state alone, the same each time.");
   }

   void Apply(IReadOnlyList<Refiling> refilings) => _filingMonitor.Locked(() =>
   {
      foreach(var refiling in refilings)
      {
         foreach(var leftKey in refiling.OldKeys.Except(refiling.NewKeys))
         {
            ITaggregateReference<TTaggregate>[] remaining = [.._committedReferencesByKey[leftKey].Where(it => it.Id != refiling.Id)];
            if(remaining.Length == 0) _committedReferencesByKey.TryRemove(leftKey, out _);
            else _committedReferencesByKey[leftKey] = remaining;
         }

         foreach(var gainedKey in refiling.NewKeys.Except(refiling.OldKeys))
            _committedReferencesByKey[gainedKey] = [..(_committedReferencesByKey.GetValueOrDefault(gainedKey) ?? []), _store.Reference<TTaggregate>(refiling.Id)];
      }
   });

   ///<summary>How a unit of work's commit refiles one taggregate: from the keys of its committed instance to those of its new state.</summary>
   sealed class Refiling
   {
      internal Refiling(TaggregateId id, TKey[] oldKeys, TKey[] newKeys)
      {
         Id = id;
         OldKeys = oldKeys;
         NewKeys = newKeys;
      }

      internal TaggregateId Id { get; }
      internal TKey[] OldKeys { get; }
      internal TKey[] NewKeys { get; }

      ///<summary>Whether the taggregate is filed, or to be filed, under any key of this index at all.</summary>
      internal bool ChangesTheIndex => OldKeys.Length > 0 || NewKeys.Length > 0;
   }

   sealed class PreparedChange : IPreparedIndexChange
   {
      readonly TaggregateIndex<TTaggregate, TKey> _index;
      readonly IReadOnlyList<Refiling> _refilings;

      internal PreparedChange(TaggregateIndex<TTaggregate, TKey> index, IReadOnlyList<Refiling> refilings)
      {
         _index = index;
         _refilings = refilings;
      }

      public void Apply() => _index.Apply(_refilings);
   }
}
