using System;

namespace Compze.Sql.Common.EventStore.Abstractions;

public record struct AggregateEventData(Guid MessageId, int AggregateVersion, Guid AggregateId, DateTime UtcTimeStamp)
{
}