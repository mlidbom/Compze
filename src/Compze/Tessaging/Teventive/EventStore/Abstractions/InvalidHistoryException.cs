using System;

namespace Compze.Tessaging.Teventive.EventStore.Abstractions;

class InvalidHistoryException(Guid aggregateId) : Exception($"AggregateId: {aggregateId}");