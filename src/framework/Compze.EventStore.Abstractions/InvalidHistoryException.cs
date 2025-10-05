using System;

namespace Compze.EventStore.Abstractions;

class InvalidHistoryException(Guid aggregateId) : Exception($"AggregateId: {aggregateId}");