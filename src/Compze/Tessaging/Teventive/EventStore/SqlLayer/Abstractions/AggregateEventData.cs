using System;

namespace Compze.Tessaging.Teventive.EventStore.SqlLayer.Abstractions;

public record struct AggregateEventData(Guid MessageId, int AggregateVersion, Guid AggregateId, DateTime UtcTimeStamp)
{
}