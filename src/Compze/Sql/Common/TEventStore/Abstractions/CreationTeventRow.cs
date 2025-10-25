using System;

namespace Compze.Sql.Common.TEventStore.Abstractions;

public class CreationTeventRow(Guid taggregateId, Guid typeId)
{
   public Guid TaggregateId { get; } = taggregateId;
   public Guid TypeId { get; } = typeId;
}