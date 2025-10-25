using System;

namespace Compze.Abstractions.Tessaging.Teventive.EventStore.Public;

class InvalidHistoryException(Guid aggregateId) : Exception($"AggregateId: {aggregateId}");