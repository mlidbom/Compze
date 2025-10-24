using System;

namespace Compze.Abstractions.Tessaging.Teventive.Eventstore.Public;

class InvalidHistoryException(Guid aggregateId) : Exception($"AggregateId: {aggregateId}");