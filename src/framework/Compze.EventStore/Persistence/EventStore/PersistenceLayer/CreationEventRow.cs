using System;

namespace Compze.Persistence.EventStore.PersistenceLayer;

public class CreationEventRow(Guid aggregateId, Guid typeId)
{
   public Guid AggregateId { get; } = aggregateId;
   public Guid TypeId { get; } = typeId;
}