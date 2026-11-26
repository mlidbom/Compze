using Compze.Abstractions;
using Compze.Tessaging;

namespace Compze.Teventive.TeventStore.Abstractions._internal.SqlLayer.Abstractions;

interface ITeventStoreSqlLayer
{
   void SetupSchemaIfDatabaseUnInitialized();

   IReadOnlyList<TeventDataRow> GetTaggregateHistory(TaggregateId taggregateId, bool takeWriteLock, int startAfterInsertedVersion = 0);

   ///<summary>As <see cref="GetTaggregateHistory"/> taking the write lock, waiting for the lock, and reading, holding no thread.</summary>
   Task<IReadOnlyList<TeventDataRow>> GetTaggregateHistoryForUpdateAsync(TaggregateId taggregateId, int startAfterInsertedVersion);
   IEnumerable<TeventDataRow> StreamTevents(int batchSize);
   IReadOnlyList<TaggregateTeventTypeRow> ListTaggregateIdsInCreationOrder();

   ///<summary>The type of each taggregate's last effective tevent, one row a taggregate, in no particular order.</summary>
   IReadOnlyList<TaggregateTeventTypeRow> ListTheLastTeventOfEachTaggregate();
   void InsertSingleTaggregateTevents(IReadOnlyList<TeventDataRow> tevents);
   void DeleteTaggregate(TaggregateId taggregateId);
   void UpdateEffectiveVersions(IReadOnlyList<VersionSpecification> versions);

   TeventNeighborhood LoadTeventNeighborHood(TessageId teventId);
}