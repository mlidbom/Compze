using Compze.Abstractions;
using Compze.Contracts;
using Compze.Teventive.TaggregateStores._internal;
using Compze.Teventive.Taggregates.Tevents;
using Compze.Teventive.TeventStore.Abstractions;
using static Compze.Contracts.Contract;

namespace Compze.Teventive.TeventStore._private;

///<summary>The <see cref="ITeventStoreReader"/> of a container: the histories the tevent store keeps.</summary>
sealed class TeventStoreReader : ITeventStoreReader
{
   readonly TaggregateTeventStore _store;

   internal TeventStoreReader(TaggregateTeventStore store) => _store = store;

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
