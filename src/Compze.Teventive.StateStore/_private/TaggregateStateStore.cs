using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Compze.Abstractions;
using Compze.Contracts;
using Compze.Teventive.TaggregateStores;
using Compze.Teventive.TaggregateStores._internal;
using Compze.Teventive.Taggregates._internal;
using Compze.Teventive._internal;
using Compze.Teventive.StateStore._private.Storage;
using Compze.Threading;
using static Compze.Contracts.Contract;

namespace Compze.Teventive.StateStore._private;

///<summary>The state store: the committed taggregates, as read only instances that any thread reads without a lock, their indexes,<br/>
/// the lock each one is changed under, and the storage that keeps what is committed. What one unit of work changes is in its<br/>
/// <see cref="StateStoreSession"/>, until the unit of work commits.</summary>
sealed class TaggregateStateStore : ITaggregateStore, IStateStore
{
   readonly TaggregateUnitsOfWork _unitsOfWork;
   readonly IReadOnlyDictionary<Type, SnapshotableTaggregateClass> _declaredClasses;
   readonly ConcurrentDictionary<TaggregateId, object> _references = new();
   readonly IMonitor _loadingMonitor = IMonitor.New();
   readonly UnitsOfWorkAndReloadsGate _gate = new();

   //Replaced whole by a reload, so that a reader sees either what was committed before it or what it loaded.
   volatile ConcurrentDictionary<TaggregateId, ITaggregate> _committedTaggregates = new();
   volatile bool _hasLoaded;

   internal TaggregateStateStore(TaggregateUnitsOfWork unitsOfWork,
                                 IEnumerable<SnapshotableTaggregateClass> declaredClasses,
                                 IEnumerable<Func<TaggregateStateStore, ITaggregateIndexMaintenance>> indexes,
                                 ITaggregateStorage storage)
   {
      _unitsOfWork = unitsOfWork;
      _declaredClasses = declaredClasses.ToDictionary(it => it.Class);
      TaggregateClasses = [.._declaredClasses.Keys];
      Storage = storage;
      Indexes = [..indexes.Select(createIndex => createIndex(this))];
   }

   public string Name => "the state store";

   public IReadOnlyList<Type> TaggregateClasses { get; }

   internal ITaggregateStorage Storage { get; }

   internal IReadOnlyList<ITaggregateIndexMaintenance> Indexes { get; }

   internal TaggregateUpdateLocks UpdateLocks { get; } = new();

   ///<summary>The index of exactly <typeparamref name="TTaggregate"/> by exactly <typeparamref name="TKey"/>: not one whose interface is<br/>
   /// merely assignable to it, such as an index of a subclass beside one of its base class.</summary>
   internal ITaggregateIndex<TTaggregate, TKey> Index<TTaggregate, TKey>() where TTaggregate : class, ITaggregate where TKey : notnull =>
      Indexes.OfType<TaggregateIndex<TTaggregate, TKey>>().Single();

   ///<summary>Inside a unit of work, the taggregate as it left it: its copy, or none when it deleted it. Otherwise the committed instance.</summary>
   public bool TryGet<TTaggregate>(TaggregateId id, [NotNullWhen(true)] out TTaggregate? taggregate) where TTaggregate : class, ITaggregate
   {
      EnsureLoaded();
      var found = AmbientSession is {} session && session.TryGetHeld(id, out var held) ? held : _committedTaggregates.GetValueOrDefault(id);
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
      _references.GetOrAdd(id, static (theId, store) => new TaggregateReference<TTaggregate>(store, store._unitsOfWork, theId), this) as ITaggregateReference<TTaggregate>
   ?? new TaggregateReference<TTaggregate>(this, _unitsOfWork, id);

   public ITaggregateStoreSession NewSession(TaggregateUnitOfWork unitOfWork)
   {
      EnsureLoaded();
      _gate.EnterUnitOfWork();
      return new StateStoreSession(this, unitOfWork, leaveTheGate: _gate.LeaveUnitOfWork);
   }

   public ReloadedTaggregates ReloadFromStorage() => ReloadFromStorageAfter(changeToTheStorage: () => {});

   public ReloadedTaggregates ReloadFromStorageAfter(Action changeToTheStorage)
   {
      State.Assert(Storage is not NoStorage,
                   () => "This state store has no storage to reload from: it was composed without one, such as StoredInFiles, so it keeps its taggregates in memory only, and a reload would remove every one of them.");
      State.Assert(AmbientSession == null, () => "A unit of work that has changed taggregates of the state store can't reload it: the reload waits for every such unit of work to complete, this one included.");
      return _gate.WhileNoUnitOfWorkIsOpen(() =>
      {
         changeToTheStorage();
         return _loadingMonitor.Locked(() =>
         {
            var before = _hasLoaded ? _committedTaggregates : new ConcurrentDictionary<TaggregateId, ITaggregate>();
            var stored = Storage.LoadAll();
            var loaded = Install(stored);
            return new ReloadedTaggregates(added: [..loaded.Keys.Where(id => !before.ContainsKey(id))],
                                           changed: [..stored.Where(it => before.TryGetValue(it.Id, out var held) && HoldsAnotherState(it, held)).Select(it => it.Id)],
                                           removed: [..before.Keys.Where(id => !loaded.ContainsKey(id))]);
         });
      });
   }

   ///<summary>Whether <paramref name="loaded"/> holds another state than <paramref name="held"/>, the instance the store held of the<br/>
   /// taggregate: another class, or another state as the storage writes state. A snapshot of <paramref name="held"/> is taken only<br/>
   /// when the storage doesn't know the state unchanged since the store last read or wrote it.</summary>
   bool HoldsAnotherState(TaggregateSnapshot loaded, ITaggregate held) =>
      loaded.TaggregateClass != held.GetType()
   || !loaded.IsUnchangedSinceTheStoreLastReadOrWroteIt && !Storage.HoldTheSameState(loaded, DeclaredClassOf(held.GetType()).TakeSnapshot(held));

   public void WriteEverythingAnew()
   {
      State.Assert(Storage is not NoStorage,
                   () => "This state store has no storage to write its taggregates to: it was composed without one, such as StoredInFiles, so it keeps its taggregates in memory only.");
      State.Assert(AmbientSession == null, () => "A unit of work that has changed taggregates of the state store can't have it write every taggregate anew: that waits for every such unit of work to complete, this one included.");
      _gate.WhileNoUnitOfWorkIsOpen(() => _loadingMonitor.Locked(() =>
      {
         if(!_hasLoaded) LoadEverything();
         Storage.WriteEverythingAnew([.._committedTaggregates.Select(it => SnapshotOf(it.Value))]);
      }));
   }

   TaggregateSnapshot SnapshotOf(ITaggregate taggregate) => new(taggregate.GetType(), taggregate.Id, DeclaredClassOf(taggregate.GetType()).TakeSnapshot(taggregate));

   ///<summary>Inside a unit of work, as it left them: the instances of the class it holds, and the committed ones it doesn't hold.<br/>
   /// Otherwise the committed ones.</summary>
   public IReadOnlyCollection<ITaggregateReference<TTaggregate>> All<TTaggregate>() where TTaggregate : class, ITaggregate
   {
      EnsureLoaded();
      //Enumerated, not read through Values, which takes every lock of the dictionary and copies every taggregate of every class.
      var committedIds = _committedTaggregates.Where(it => it.Value is TTaggregate).Select(it => it.Key);
      var ids = AmbientSession is {} session
                   ? committedIds.Where(id => !session.Holds(id)).Concat(session.HeldInstancesOf<TTaggregate>().Select(it => it.Id))
                   : committedIds;
      return [..ids.Select(Reference<TTaggregate>)];
   }

   ///<summary>This store's session in the unit of work of the ambient transaction, if that unit of work has needed the store yet.<br/>
   /// Allocates nothing.</summary>
   internal StateStoreSession? AmbientSession => (StateStoreSession?)_unitsOfWork.Ambient?.ExistingSessionOf(this);

   internal ITaggregate? CommittedInstanceOf(TaggregateId id) => _committedTaggregates.GetValueOrDefault(id);

   internal SnapshotableTaggregateClass DeclaredClassOf(Type taggregateClass)
   {
      State.Assert(_declaredClasses.ContainsKey(taggregateClass),
                   () => $"The state store keeps no {taggregateClass.FullName}: it keeps {string.Join(", ", TaggregateClasses.Select(it => it.FullName))}. Declare the class where the store is composed.");
      return _declaredClasses[taggregateClass];
   }

   ///<summary>Makes <paramref name="taggregate"/> the committed instance, read only from now on.</summary>
   internal void MakeCommitted(ITaggregate taggregate)
   {
      ((ITaggregateStoreInternals)taggregate).MakeReadOnly();
      _committedTaggregates[taggregate.Id] = taggregate;
   }

   internal void Remove(TaggregateId id) => _committedTaggregates.TryRemove(id, out _);

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
   /// the storage has just loaded.</summary>
   ConcurrentDictionary<TaggregateId, ITaggregate> Install(IReadOnlyList<TaggregateSnapshot> stored)
   {
      var loaded = new ConcurrentDictionary<TaggregateId, ITaggregate>(
         stored.Select(it => it.RestoredTaggregate ?? DeclaredClassOf(it.TaggregateClass).Restore(it.Id, it.Snapshot))
               .Select(it => KeyValuePair.Create(it.Id, it)));
      foreach(var taggregate in loaded.Values) ((ITaggregateStoreInternals)taggregate).MakeReadOnly();
      foreach(var index in Indexes) index.FileExactly(loaded.Values);
      _committedTaggregates = loaded;
      _hasLoaded = true;
      return loaded;
   }
}
