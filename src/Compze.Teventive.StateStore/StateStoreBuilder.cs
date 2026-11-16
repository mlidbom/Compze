using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Teventive.StateStore.Storage;
using Compze.Teventive.StateStore._private;
using Compze.Teventive.TaggregateStores._internal;
using Compze.Teventive.Taggregates;
using Compze.Teventive.Taggregates._internal;
using static Compze.Contracts.Contract;

namespace Compze.Teventive.StateStore;

///<summary>Declares, where a state store is composed, the taggregate classes it keeps, its indexes, and where it stores what it commits.</summary>
public sealed class StateStoreBuilder
{
   readonly IComponentRegistrar _registrar;
   readonly Dictionary<Type, Func<IRootResolver, SnapshotableTaggregateClass>> _taggregateClasses = new();
   readonly List<Func<TaggregateStateStore, ITaggregateIndexMaintenance>> _indexes = [];
   Func<IRootResolver, ITaggregateStorage>? _storage;

   internal StateStoreBuilder(IComponentRegistrar registrar) => _registrar = registrar;

   ///<summary>Declares that the store keeps the taggregate class that <paramref name="createEmpty"/> builds new, empty instances of,<br/>
   /// for the store to restore from snapshots. The class and its snapshot type are inferred from the instances it builds.</summary>
   ///<remarks>A factory, not a parameterless constructor, so that the taggregates can be handed what they need. To hand them a<br/>
   /// service of the container, use <see cref="Taggregate{TTaggregate,TSnapshot,TService}"/>.<br/>
   /// <paramref name="snapshotsAreEqual"/> tells whether two snapshots hold the same state, for
   /// <see cref="IStateStore.ReloadFromStorage"/> to report what changed. Without it, the snapshot type's own Equals does, and must<br/>
   /// be overridden.</remarks>
   public StateStoreBuilder Taggregate<TTaggregate, TSnapshot>(Func<ISnapshotable<TTaggregate, TSnapshot>> createEmpty, Func<TSnapshot, TSnapshot, bool>? snapshotsAreEqual = null)
      where TTaggregate : class, ITaggregate, ISnapshotable<TTaggregate, TSnapshot> =>
      Declare<TTaggregate>(_ => SnapshotableTaggregateClass.For(createEmpty, snapshotsAreEqual));

   ///<summary>As <see cref="Taggregate{TTaggregate,TSnapshot}"/>, for a factory that builds the empty instances with a service of the<br/>
   /// container, such as the services every note is built with: <c>store.Taggregate((NoteServices services) => new VocabNote(services))</c>.</summary>
   ///<remarks>The service is resolved from the container's root each time an instance is built.</remarks>
   public StateStoreBuilder Taggregate<TTaggregate, TSnapshot, TService>(Func<TService, ISnapshotable<TTaggregate, TSnapshot>> createEmpty, Func<TSnapshot, TSnapshot, bool>? snapshotsAreEqual = null)
      where TTaggregate : class, ITaggregate, ISnapshotable<TTaggregate, TSnapshot> where TService : class =>
      Declare<TTaggregate>(resolver => SnapshotableTaggregateClass.For(() => createEmpty(resolver.Resolve<TService>()), snapshotsAreEqual));

   StateStoreBuilder Declare<TTaggregate>(Func<IRootResolver, SnapshotableTaggregateClass> declare)
   {
      State.Assert(!_taggregateClasses.ContainsKey(typeof(TTaggregate)), () => $"{typeof(TTaggregate).FullName} is declared twice for the state store.");
      _taggregateClasses.Add(typeof(TTaggregate), declare);
      return this;
   }

   ///<summary>Declares an <see cref="ITaggregateIndex{TTaggregate,TKey}"/>, which files each <typeparamref name="TTaggregate"/> under the<br/>
   /// keys <paramref name="keysOf"/> gives from its state. The container then resolves it.</summary>
   public StateStoreBuilder Index<TTaggregate, TKey>(Func<TTaggregate, IEnumerable<TKey>> keysOf) where TTaggregate : class, ITaggregate where TKey : notnull
   {
      _indexes.Add(store => new TaggregateIndex<TTaggregate, TKey>(store, keysOf));
      _registrar.Register(Singleton.For<ITaggregateIndex<TTaggregate, TKey>>().CreatedBy((TaggregateStateStore store) => store.Index<ITaggregateIndex<TTaggregate, TKey>>()));
      return this;
   }

   ///<summary>Declares where the store keeps what it commits. Without it, the store keeps its taggregates in memory only.</summary>
   public StateStoreBuilder StoredIn(ITaggregateStorage storage) => DeclareStorage(_ => storage);

   ///<summary>As <see cref="StoredIn(ITaggregateStorage)"/>, for a storage built with a service of the container, such as one that<br/>
   /// knows where the consumer's files are: <c>store.StoredIn((CorpusPaths paths) => new CorpusFiles(paths))</c>.</summary>
   ///<remarks>The service is resolved from the container's root when the store is built, at its first use, so a service that a<br/>
   /// host can only give once it has started is there.</remarks>
   public StateStoreBuilder StoredIn<TService>(Func<TService, ITaggregateStorage> storageWith) where TService : class =>
      DeclareStorage(resolver => storageWith(resolver.Resolve<TService>()));

   StateStoreBuilder DeclareStorage(Func<IRootResolver, ITaggregateStorage> storage)
   {
      State.Assert(_storage == null, () => "The state store's storage is declared twice.");
      _storage = storage;
      return this;
   }

   internal TaggregateStateStore Build(TaggregateUnitsOfWork unitsOfWork, IRootResolver resolver) =>
      new(unitsOfWork, _taggregateClasses.Values.Select(declare => declare(resolver)), _indexes, _storage?.Invoke(resolver) ?? new NoStorage());
}
