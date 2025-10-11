using System;

namespace Compze.Tessaging.Teventive.EventStore.SqlLayer.Abstractions;

public class CreationEventRow(Guid aggregateId, Guid typeId)
{
   public Guid AggregateId { get; } = aggregateId;
   public Guid TypeId { get; } = typeId;
}