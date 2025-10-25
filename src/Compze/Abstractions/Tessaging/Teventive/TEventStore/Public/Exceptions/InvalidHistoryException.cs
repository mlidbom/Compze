using System;

namespace Compze.Abstractions.Tessaging.Teventive.TEventStore.Public.Exceptions;

class InvalidHistoryException(Guid taggregateId) : Exception($"TaggregateId: {taggregateId}");