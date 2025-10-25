using System;

namespace Compze.Sql.Common.TeventStore.Abstractions;

public class CreationTeventRow(Guid aggregateId, Guid typeId)
{
   public Guid AggregateId { get; } = aggregateId;
   public Guid TypeId { get; } = typeId;
}