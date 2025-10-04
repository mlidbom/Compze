using System;

namespace Compze.Abstractions.Internal.Persistence.EventStore;

class InvalidHistoryException(Guid aggregateId) : Exception($"AggregateId: {aggregateId}");