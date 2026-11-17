using System.Diagnostics.CodeAnalysis;
using Compze.Abstractions;
using Compze.Contracts;
using Compze.Teventive.TaggregateStores;
using Compze.Teventive.TaggregateStores._internal;
using Compze.Teventive.Taggregates.Tevents;
using Compze.Teventive.TeventStore.Abstractions;
using static Compze.Contracts.Contract;

namespace Compze.Teventive.TeventStore._private;

///<summary>The <see cref="ITeventStoreReader"/> of a container: the container's <see cref="ITaggregateStoreReader"/>, and the histories<br/>
/// the tevent store keeps.</summary>
sealed class TeventStoreReader : ITeventStoreReader
{
   readonly ITaggregateStoreReader _taggregates;
   readonly TaggregateTeventStore _store;

   internal TeventStoreReader(ITaggregateStoreReader taggregates, TaggregateTeventStore store)
   {
      _taggregates = taggregates;
      _store = store;
   }

   public TTaggregate Get<TTaggregate>(TaggregateId id) where TTaggregate : class, ITaggregate => _taggregates.Get<TTaggregate>(id);

   public bool TryGet<TTaggregate>(TaggregateId id, [NotNullWhen(true)] out TTaggregate? taggregate) where TTaggregate : class, ITaggregate =>
      _taggregates.TryGet(id, out taggregate);

   public ITaggregateReference<TTaggregate> Reference<TTaggregate>(TaggregateId id) where TTaggregate : class, ITaggregate => _taggregates.Reference<TTaggregate>(id);

   public IReadOnlyList<ITaggregateTevent<ITaggregateTeventWithIdentity>> GetHistory(TaggregateId taggregateId) => _store.TeventStore.GetTaggregateHistory(taggregateId);

   public TTaggregate GetReadonlyCopyOfVersion<TTaggregate>(TaggregateId taggregateId, int version) where TTaggregate : class, ITaggregate<ITaggregateTeventWithIdentity>
   {
      Argument.Assert(version > 0);
      var history = GetHistory(taggregateId);
      if(history.Count == 0) throw new TaggregateNotFoundException(taggregateId);
      Argument.Assert(version <= history.Count, () => $"Taggregate {taggregateId} has no version {version}: its latest is {history.Count}.");

      return _store.ReadOnlyVersionFrom<TTaggregate>([..history.Where(it => it.Tevent.TaggregateVersion <= version)]);
   }
}
