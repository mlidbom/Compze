using Compze.Abstractions;
using Compze.Teventive.TaggregateStores;
using Compze.Teventive.Taggregates.Tevents;

namespace Compze.Teventive.TeventStore.Abstractions;

///<summary>The <see cref="ITaggregateStoreReader"/>, with what only a store that keeps taggregates as their history can read: a<br/>
/// taggregate's history, and the taggregate as it was at a version of it.</summary>
///<remarks>Its <see cref="ITaggregateStoreReader.Get{TTaggregate}"/> reads every store, as the <see cref="ITaggregateStoreReader"/> does.<br/>
/// Outside a unit of work, a taggregate of the tevent store comes back built from its history, read only: publishing on it throws.</remarks>
public interface ITeventStoreReader : ITaggregateStoreReader
{
   ///<summary>The taggregate's persisted history: its wrapped tevents exactly as published and stored, publisher identity included.</summary>
   IReadOnlyList<ITaggregateTevent<ITaggregateTeventWithIdentity>> GetHistory(TaggregateId taggregateId);

   ///<summary>The taggregate <paramref name="taggregateId"/> as it was at <paramref name="version"/>, built from its history up to that<br/>
   /// version. Read only: publishing on it throws.</summary>
   TTaggregate GetReadonlyCopyOfVersion<TTaggregate>(TaggregateId taggregateId, int version) where TTaggregate : class, ITaggregate<ITaggregateTeventWithIdentity>;
}
