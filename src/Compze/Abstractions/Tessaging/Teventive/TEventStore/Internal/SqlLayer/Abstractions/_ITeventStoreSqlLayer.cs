using System;
using System.Collections.Generic;

namespace Compze.Core.Tessaging.Teventive.TeventStore.Internal.SqlLayer.Abstractions;

public interface ITeventStoreSqlLayer
{
   void SetupSchemaIfDatabaseUnInitialized();

   IReadOnlyList<TeventDataRow> GetTaggregateHistory(Guid taggregateId, bool takeWriteLock, int startAfterInsertedVersion = 0);
   IEnumerable<TeventDataRow> StreamTevents(int batchSize);
   IReadOnlyList<CreationTeventRow> ListTaggregateIdsInCreationOrder();
   void InsertSingleTaggregateTevents(IReadOnlyList<TeventDataRow> tevents);
   void DeleteTaggregate(Guid taggregateId);
   void UpdateEffectiveVersions(IReadOnlyList<VersionSpecification> versions);

   TeventNeighborhood LoadTeventNeighborHood(Guid teventId);
}