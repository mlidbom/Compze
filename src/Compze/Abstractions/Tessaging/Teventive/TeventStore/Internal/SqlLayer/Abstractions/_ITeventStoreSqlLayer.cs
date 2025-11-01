using System.Collections.Generic;
using Compze.Core.Public;

namespace Compze.Core.Tessaging.Teventive.TeventStore.Internal.SqlLayer.Abstractions;

public interface ITeventStoreSqlLayer
{
   void SetupSchemaIfDatabaseUnInitialized();

   IReadOnlyList<TeventDataRow> GetTaggregateHistory(TaggregateId taggregateId, bool takeWriteLock, int startAfterInsertedVersion = 0);
   IEnumerable<TeventDataRow> StreamTevents(int batchSize);
   IReadOnlyList<CreationTeventRow> ListTaggregateIdsInCreationOrder();
   void InsertSingleTaggregateTevents(IReadOnlyList<TeventDataRow> tevents);
   void DeleteTaggregate(TaggregateId taggregateId);
   void UpdateEffectiveVersions(IReadOnlyList<VersionSpecification> versions);

   TeventNeighborhood LoadTeventNeighborHood(TessageId teventId);
}