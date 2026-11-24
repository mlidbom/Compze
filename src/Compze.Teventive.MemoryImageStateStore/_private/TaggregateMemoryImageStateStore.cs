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
sealed class TaggregateMemoryImageStateStore : ITaggregateStore, IMemoryImageStateStore
{
   readonly TaggregateUnitsOfWork _unitsOfWork;
   readonly IReadOnlyDictionary<Type, SnapshotableTaggregateClass> _declaredClasses;
   readonly ConcurrentDictionary<TaggregateId, object> _references = new();
   readonly IMonitor _loadingMonitor = IMonitor.New(); //Taken by a load and by catching up, so that neither installs what it read over what the other did.
   readonly UnitsOfWorkAndReloadsGate _gate = new();
   volatile bool _hasLoaded;

   //Read and written under _loadingMonitor.
   CommitNumber _lastCommitNumberSeen = CommitNumber.BeforeTheFirstCommit;

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

   ///<remarks>Catches up first, so that what the unit of work reads, and decides from, is what the other memory images had committed<br/>
   /// when it began changing the store. Inside the gate, so that a unit of work that begins during a load waits for it there.</remarks>
   public ITaggregateStoreSession NewSession(TaggregateUnitOfWork unitOfWork)
   {
      EnsureLoaded();
      _gate.EnterUnitOfWork();
      try
      {
         CatchUp();
      }
      catch
      {
         _gate.LeaveUnitOfWork(); //No session holds the gate yet to leave it when the unit of work completes.
         throw;
      }

      return new MemoryImageStateStoreSession(this, unitOfWork, leaveTheGate: _gate.LeaveUnitOfWork);
   }

   public ChangesFoundInStorage LoadFromStorage(Action? changeTheStorageFirst = null)
   {
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
            var changed = stored.Snapshots.Where(it => before.TryGetValue(it.Id, out var held) && HoldsAnotherState(it, held)).Select(it => it.Id).ToList();
            var loadedIds = stored.Snapshots.Select(it => it.Id).ToHashSet();
            Install(stored);
            return new ChangesFoundInStorage(added: [..loadedIds.Where(id => !before.ContainsKey(id))],
                                           changed: changed,
                                           removed: [..before.Keys.Where(id => !loadedIds.Contains(id))]);
         });
      });
   }

   ///<summary>Whether <paramref name="loaded"/> holds another state than <paramref name="held"/>, the instance the store held of the<br/>
   /// taggregate: another class, another commit, where the storage numbers its commits, or else another state as the storage writes<br/>
   /// state. A snapshot of <paramref name="held"/> is taken only when the storage doesn't know the state unchanged since the store last<br/>
   /// read or wrote it.</summary>
   bool HoldsAnotherState(TaggregateSnapshot loaded, ITaggregate held) =>
      loaded.TaggregateClass != held.GetType()
   || (loaded.CommittedAs is {} committedAs
          ? committedAs != Committed.CommitNumberOf(loaded.Id)
          : !loaded.IsUnchangedSinceTheStoreLastReadOrWroteIt && !Storage.HoldTheSameState(loaded, DeclaredClassOf(held.GetType()).TakeSnapshot(held)));

   public void WriteAllToStorage()
   {
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

   ///<summary>Takes the taggregate <paramref name="id"/>'s lock in the storage, once a unit of work of this store holds its lock in<br/>
   /// memory, and installs the taggregate as stored if another memory image has committed it after the commit held.</summary>
   internal void LockInTheStorageForUpdate(TaggregateId id)
   {
      if(Storage.LockForUpdate(id, Committed.CommitNumberOf(id)) is {} stored) InstallFound([stored]);
   }

   ///<summary>Installs what the commits after the last one this memory image has seen stored, from other memory images: except the<br/>
   /// taggregates a unit of work of this memory image holds, or waits for, the lock of. While it holds the lock, no other memory<br/>
   /// image can commit the taggregate, so a newer state of it is the unit of work's own commit, which that commit installs. The<br/>
   /// last commit number seen stays below such a taggregate's, so that the next catch-up reads it again, should the unit of work<br/>
   /// never get it.</summary>
   void CatchUp() => _loadingMonitor.Locked(() =>
   {
      var committed = Storage.CommittedAfter(_lastCommitNumberSeen);
      var held = committed.Taggregates.Where(it => UpdateLocks.IsHeldOrWaitedFor(it.Id)).ToList();
      InstallFound([..committed.Taggregates.Except(held)]);
      _lastCommitNumberSeen = held.Count == 0
                                 ? committed.HoldingEveryCommitThrough
                                 : new CommitNumber(Math.Min(committed.HoldingEveryCommitThrough.Value, held.Min(it => it.CommittedAs.Value) - 1));
   });

   ///<summary>Restores each of <paramref name="found"/> that isn't deleted, and installs each that is newer than what is held.</summary>
   void InstallFound(IReadOnlyList<StoredTaggregate> found)
   {
      if(found.Count == 0) return;
      Committed.InstallFound([..found.Select(it => (Stored: it, Restored: it.Snapshot is {} snapshot ? RestoredFrom(snapshot) : null))]);
   }

   ITaggregate RestoredFrom(TaggregateSnapshot snapshot) => snapshot.RestoredTaggregate ?? DeclaredClassOf(snapshot.TaggregateClass).Restore(snapshot.Id, snapshot.Snapshot);

   ///<summary>Fills the store from its storage, once, at its first use.</summary>
   internal void EnsureLoaded()
   {
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
   void Install(LoadedTaggregates stored)
   {
      Committed.ReplaceWith([..stored.Snapshots.Select(it => (RestoredFrom(it), it.CommittedAs))]);
      if(stored.HoldingEveryCommitThrough is {} holdingEveryCommitThrough) _lastCommitNumberSeen = holdingEveryCommitThrough;
      _hasLoaded = true;
   }
}
