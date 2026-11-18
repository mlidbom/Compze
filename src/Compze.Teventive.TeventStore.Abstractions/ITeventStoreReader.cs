using Compze.Abstractions;
using Compze.Teventive.TaggregateStores;
using Compze.Teventive.Taggregates.Tevents;

namespace Compze.Teventive.TeventStore.Abstractions;

///<summary>Reads what only the tevent store can, since it keeps each taggregate as its history: a taggregate's history, and the<br/>
/// taggregate as it was at a version of it.</summary>
///<remarks>A taggregate as it is now is read through the <see cref="ITaggregateStoreReader"/>, which reads every store.</remarks>
public interface ITeventStoreReader
{
   ///<summary>The taggregate's persisted history: its wrapped tevents exactly as published and stored, publisher identity included.</summary>
   IReadOnlyList<ITaggregateTevent<ITaggregateTeventWithIdentity>> GetHistory(TaggregateId taggregateId);

   ///<summary>The taggregate <paramref name="taggregateId"/> as it was at <paramref name="version"/>, built from its history up to that<br/>
   /// version. Read only: publishing on it throws.</summary>
   TTaggregate GetReadonlyCopyOfVersion<TTaggregate>(TaggregateId taggregateId, int version) where TTaggregate : class, ITaggregate<ITaggregateTeventWithIdentity>;
}
