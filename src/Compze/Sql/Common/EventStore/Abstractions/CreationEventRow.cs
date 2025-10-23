using System;

namespace Compze.Sql.Common.EventStore.Abstractions;

public class CreationEventRow(Guid aggregateId, Guid typeId)
{
   public Guid AggregateId { get; } = aggregateId;
   public Guid TypeId { get; } = typeId;
}