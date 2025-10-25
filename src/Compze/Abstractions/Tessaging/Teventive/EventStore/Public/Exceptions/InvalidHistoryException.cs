using System;

namespace Compze.Abstractions.Tessaging.Teventive.EventStore.Public.Exceptions;

class InvalidHistoryException(Guid aggregateId) : Exception($"AggregateId: {aggregateId}");