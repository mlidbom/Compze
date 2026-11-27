using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Compze.Abstractions;
using Compze.Contracts;
using Compze.Teventive.TaggregateStores;
using Compze.Teventive.TaggregateStores._internal;
using Compze.Teventive.Taggregates._internal;
using Compze.Teventive.MemoryImageStateStore._private.Storage;
using Compze.Threading;
using static Compze.Contracts.Contract;

namespace Compze.Teventive.MemoryImageStateStore._private;

///<summary>The memory image state store: the committed taggregates, as read only instances that any thread reads without a lock, their indexes,<br/>
/// the lock each one is changed under, and the storage that keeps what is committed. What one unit of work changes is in its<br/>
/// <see cref="MemoryImageStateStoreSession"/>, until the unit of work commits.</summary>
sealed class TaggregateMemoryImageStateStore : ITaggregateStore, IMemoryImageStateStore, IDisposable
{
   readonly TaggregateUnitsOfWork _unitsOfWork;
   readonly IReadOnlyDictionary<Type, SnapshotableTaggregateClass> _declaredClasses;
   readonly ConcurrentDictionary<TaggregateId, object> _references = new();
   readonly IMonitor _loadingMonitor = IMonitor.New(); //Taken by every load, and by writing everything anew, so that none installs what it read over what another did.
   readonly UnitsOfWorkAndReloadsGate _gate = new();
   volatile bool _hasLoaded;
   volatile bool _isDisposed;

   internal TaggregateMemoryImageStateStore(TaggregateUnitsOfWork unitsOfWork,
                                 IEnumerable<SnapshotableTaggregateClass> declaredClasses,
                                 IEnumerable<Func<TaggregateMemoryImageStateStore, ITaggregateIndexMaintenance>> indexes,
                                 ITaggregateStorage storage)
   {
      _unitsOfWork = unitsOfWork;
      _declaredClasses = declaredClasses.ToDictionary(it => it.Class);
      TaggregateClasses = [.._declaredClasses.Keys];
      Storage = storage;
      Indexes = [..indexes.Select(createIndex => createIndex(this))];
      Committed = new CommittedTaggregates(Indexes);
   }

   ///<summary>Refuses every use from now on, then releases the storage, and with it the storage's lock, for the next memory image to take,<br/>
   /// once no write of this one can still land: see <see cref="StorageLock"/>.</summary>
   ///<remarks>A unit of work under way as disposing begins: one that has prepared its write commits or rolls back as it would have, and one<br/>
   /// that hasn't gets, adds and prepares nothing more, so it rolls back.</remarks>
   public void Dispose()
   {
      _isDisposed = true;
      Storage.Dispose();
   }

   ///<summary>Refuses a use of a store that is disposed: what it holds in memory may no longer be what its storage holds, since another memory<br/>
   /// image may hold the storage by now.</summary>
   internal void AssertNotDisposed() =>
      State.Assert(!_isDisposed, () => "The memory image state store is disposed, as its container is, so it is used no more: another memory image may hold its storage by now, and what this one holds may no longer be what the storage holds.");

   public string Name => "the memory image state store";

   public IReadOnlyList<Type> TaggregateClasses { get; }

   internal ITaggregateStorage Storage { get; }

   internal IReadOnlyList<ITaggregateIndexMaintenance> Indexes { get; }

   internal TaggregateUpdateLocks UpdateLocks { get; } = new();

   internal CommittedTaggregates Committed { get; }

   ///<summary>The index of exactly <typeparamref name="TTaggregate"/> by exactly <typeparamref name="TKey"/>: not one whose interface is<br/>
   /// merely assignable to it, such as an index of a subclass beside one of its base class.</summary>
   internal ITaggregateIndex<TTaggregate, TKey> Index<TTaggregate, TKey>() where TTaggregate : class, ITaggregate where TKey : notnull =>
      Indexes.OfType<TaggregateIndex<TTaggregate, TKey>>().Single();

   ///<summary>Inside a unit of work, the taggregate as it left it: its copy, or none when it deleted it. Otherwise the committed instance.</summary>
   public bool TryGet<TTaggregate>(TaggregateId id, [NotNullWhen(true)] out TTaggregate? taggregate) where TTaggregate : class, ITaggregate
   {
      EnsureLoaded();
      var found = AmbientSession is {} session && session.TryGetHeld(id, out var held) ? held : Committed.InstanceOf(id);
      if(found == null)
      {
         taggregate = null;
         return false;
      }

      taggregate = found as TTaggregate;
      State.Assert(taggregate != null, () => $"Taggregate {id} is a {found.GetType().FullName}, not a {typeof(TTaggregate).FullName}.");
      return true;
   }

   ///<summary>One reference for each taggregate, made at the first request, so that index lookups allocate none. A request for it as<br/>
   /// a class the first reference can't be cast to gets a new one, which is equal to it.</summary>
   public ITaggregateReference<TTaggregate> Reference<TTaggregate>(TaggregateId id) where TTaggregate : class, ITaggregate =>
      _references.GetOrAdd(id, static (theId, store) => new TaggregateReference<TTaggregate>(store, theId), this) as ITaggregateReference<TTaggregate>
   ?? new TaggregateReference<TTaggregate>(this, id);

   ///<remarks>Enters the gate, so that a unit of work that begins during a load waits for it there.</remarks>
   public ITaggregateStoreSession NewSession(TaggregateUnitOfWork unitOfWork)
   {
      EnsureLoaded();
      _gate.EnterUnitOfWork();
      return new MemoryImageStateStoreSession(this, unitOfWork, leaveTheGate: _gate.LeaveUnitOfWork);
   }

   public ChangesFoundInStorage LoadFromStorage(Action? changeTheStorageFirst = null)
   {
      AssertNotDisposed();
      State.Assert(Storage is not NoStorage,
                   () => "This memory image state store has no storage to reload from: it was composed without one, such as StoredInJsonFiles, so it keeps its taggregates in memory only, and a reload would remove every one of them.");
      State.Assert(AmbientSession == null, () => "A unit of work that has changed taggregates of the memory image state store can't reload it: the reload waits for every such unit of work to complete, this one included.");
      return _gate.WhileNoUnitOfWorkIsOpen(() =>
      {
         changeTheStorageFirst?.Invoke();
         return _loadingMonitor.Locked(() =>
         {
            var before = _hasLoaded ? Committed.Instances : new Dictionary<TaggregateId, ITaggregate>();
            var stored = Storage.LoadAll();
            var changed = stored.Where(it => before.TryGetValue(it.Id, out var held) && HoldsAnotherState(it, held)).Select(it => it.Id).ToList();
            var loadedIds = stored.Select(it => it.Id).ToHashSet();
            Install(stored);
            return new ChangesFoundInStorage(added: [..loadedIds.Where(id => !before.ContainsKey(id))],
                                           changed: changed,
                                           removed: [..before.Keys.Where(id => !loadedIds.Contains(id))]);
         });
      });
   }

   ///<summary>Whether <paramref name="loaded"/> holds another state than <paramref name="held"/>, the instance the store held of the<br/>
   /// taggregate: another class, or another state as the storage writes state. A snapshot of <paramref name="held"/> is taken only when<br/>
   /// the storage doesn't know the state unchanged since the store last read or wrote it.</summary>
   bool HoldsAnotherState(TaggregateSnapshot loaded, ITaggregate held) =>
      loaded.TaggregateClass != held.GetType()
   || (!loaded.IsUnchangedSinceTheStoreLastReadOrWroteIt && !Storage.HoldTheSameState(loaded, DeclaredClassOf(held.GetType()).TakeSnapshot(held)));

   public void WriteAllToStorage()
   {
      AssertNotDisposed();
      State.Assert(Storage is not NoStorage,
                   () => "This memory image state store has no storage to write its taggregates to: it was composed without one, such as StoredInJsonFiles, so it keeps its taggregates in memory only.");
      State.Assert(AmbientSession == null, () => "A unit of work that has changed taggregates of the memory image state store can't have it write every taggregate anew: that waits for every such unit of work to complete, this one included.");
      _gate.WhileNoUnitOfWorkIsOpen(() => _loadingMonitor.Locked(() =>
      {
         if(!_hasLoaded) LoadEverything();
         Storage.WriteAll([..Committed.Instances.Select(it => SnapshotOf(it.Value))]);
      }));
   }

   TaggregateSnapshot SnapshotOf(ITaggregate taggregate) => new(taggregate.GetType(), taggregate.Id, DeclaredClassOf(taggregate.GetType()).TakeSnapshot(taggregate));

   ///<summary>Inside a unit of work, as it left them: the instances of the class it holds, and the committed ones it doesn't hold.<br/>
   /// Otherwise the committed ones.</summary>
   public IReadOnlyCollection<ITaggregateReference<TTaggregate>> ReferencesToAll<TTaggregate>() where TTaggregate : class, ITaggregate
   {
      EnsureLoaded();
      //Enumerated, not read through Values, which takes every lock of the dictionary and copies every taggregate of every class.
      var committedIds = Committed.Instances.Where(it => it.Value is TTaggregate).Select(it => it.Key);
      var ids = AmbientSession is {} session
                   ? committedIds.Where(id => !session.Holds(id)).Concat(session.HeldInstancesOf<TTaggregate>().Select(it => it.Id))
                   : committedIds;
      return [..ids.Select(Reference<TTaggregate>)];
   }

   ///<summary>This store's session in the unit of work of the ambient transaction, if that unit of work has needed the store yet.<br/>
   /// Allocates nothing.</summary>
   internal MemoryImageStateStoreSession? AmbientSession => (MemoryImageStateStoreSession?)_unitsOfWork.Ambient?.ExistingSessionOf(this);

   internal SnapshotableTaggregateClass DeclaredClassOf(Type taggregateClass)
   {
      State.Assert(_declaredClasses.ContainsKey(taggregateClass),
                   () => $"The memory image state store keeps no {taggregateClass.FullName}: it keeps {string.Join(", ", TaggregateClasses.Select(it => it.FullName))}. Declare the class where the store is composed.");
      return _declaredClasses[taggregateClass];
   }

   ITaggregate RestoredFrom(TaggregateSnapshot snapshot) => snapshot.RestoredTaggregate ?? DeclaredClassOf(snapshot.TaggregateClass).Restore(snapshot.Id, snapshot.Snapshot);

   ///<summary>Fills the store from its storage, once, at its first use: every read, and every unit of work's first use of the store, begins<br/>
   /// here, so it refuses them all once the store is disposed.</summary>
   internal void EnsureLoaded()
   {
      AssertNotDisposed();
      if(_hasLoaded) return;
      _loadingMonitor.Locked(() =>
      {
         if(!_hasLoaded) LoadEverything();
      });
   }

   ///<summary>Replaces the committed taggregates, and what the indexes file, with what the storage holds.</summary>
   void LoadEverything() => Install(Storage.LoadAll());

   ///<summary>Replaces the committed taggregates, and what the indexes file, with the taggregates of <paramref name="stored"/>, what<br/>
   /// the storage has just loaded. Called under <see cref="_loadingMonitor"/>.</summary>
   void Install(IReadOnlyList<TaggregateSnapshot> stored)
   {
      Committed.ReplaceWith([..stored.Select(RestoredFrom)]);
      _hasLoaded = true;
   }
}
