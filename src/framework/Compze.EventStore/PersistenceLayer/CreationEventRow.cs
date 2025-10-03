using System;

namespace Compze.EventStore.PersistenceLayer;

public class CreationEventRow(Guid aggregateId, Guid typeId)
{
   public Guid AggregateId { get; } = aggregateId;
   public Guid TypeId { get; } = typeId;
}