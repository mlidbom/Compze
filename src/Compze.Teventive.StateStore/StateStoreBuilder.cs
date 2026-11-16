using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Teventive.StateStore._private;
using Compze.Teventive.StateStore.Storage;
using Compze.Teventive.Taggregates;
using Compze.Teventive.TaggregateStores._internal;
using Compze.Contracts;
using Compze.Internals.SystemCE.LinqCE;
using static Compze.Contracts.Contract;

namespace Compze.Teventive.StateStore;

///<summary>Declares, where a state store is composed, the taggregate classes it keeps, its indexes, and where it stores what it commits.</summary>
public sealed class StateStoreBuilder
{
   readonly IComponentRegistrar _registrar;
   readonly List<DeclaredTaggregateClass> _taggregateClasses = [];
   readonly List<Func<TaggregateStateStore, ITaggregateIndexMaintenance>> _indexes = [];
   ITaggregateStorage? _storage;

   internal StateStoreBuilder(IComponentRegistrar registrar) => _registrar = registrar;

   ///<summary>Declares that the store keeps the taggregate class that <paramref name="createEmpty"/> builds new, empty instances of,<br/>
   /// for the store to restore from snapshots. The class and its snapshot type are inferred from the instances it builds.</summary>
   ///<remarks>A factory, not a parameterless constructor, so that the taggregates can be handed the services they need.</remarks>
   public StateStoreBuilder Taggregate<TTaggregate, TSnapshot>(Func<ISnapshotable<TTaggregate, TSnapshot>> createEmpty)
      where TTaggregate : class, ITaggregate, ISnapshotable<TTaggregate, TSnapshot>
   {
      State.Assert(_taggregateClasses.None(it => it.Class == typeof(TTaggregate)), () => $"{typeof(TTaggregate).FullName} is declared twice for the state store.");
      _taggregateClasses.Add(DeclaredTaggregateClass.For(createEmpty));
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
   public StateStoreBuilder StoredIn(ITaggregateStorage storage)
   {
      State.Assert(_storage == null, () => "The state store's storage is declared twice.");
      _storage = storage;
      return this;
   }

   internal TaggregateStateStore Build(TaggregateUnitsOfWork unitsOfWork) => new(unitsOfWork, _taggregateClasses, _indexes, _storage ?? new NoStorage());
}
