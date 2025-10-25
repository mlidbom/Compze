using System;

namespace Compze.Sql.Common.EventStore.Abstractions;

public record struct AggregateEventData(Guid TessageId, int AggregateVersion, Guid AggregateId, DateTime UtcTimeStamp)
{
}