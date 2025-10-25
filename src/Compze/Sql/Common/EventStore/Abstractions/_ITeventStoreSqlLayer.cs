using System;
using System.Collections.Generic;

namespace Compze.Sql.Common.TeventStore.Abstractions;

public interface ITeventStoreSqlLayer
{
   void SetupSchemaIfDatabaseUnInitialized();

   IReadOnlyList<TeventDataRow> GetAggregateHistory(Guid aggregateId, bool takeWriteLock, int startAfterInsertedVersion = 0);
   IEnumerable<TeventDataRow> StreamTevents(int batchSize);
   IReadOnlyList<CreationTeventRow> ListAggregateIdsInCreationOrder();
   void InsertSingleAggregateTevents(IReadOnlyList<TeventDataRow> tevents);
   void DeleteAggregate(Guid aggregateId);
   void UpdateEffectiveVersions(IReadOnlyList<VersionSpecification> versions);

   TeventNeighborhood LoadTeventNeighborHood(Guid teventId);
}