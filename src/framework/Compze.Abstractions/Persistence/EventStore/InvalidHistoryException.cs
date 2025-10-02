using System;

namespace Compze.Persistence.EventStore;

class InvalidHistoryException(Guid aggregateId) : Exception($"AggregateId: {aggregateId}");