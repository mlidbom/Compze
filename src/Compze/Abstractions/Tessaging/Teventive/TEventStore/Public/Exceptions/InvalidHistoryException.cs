using System;

namespace Compze.Core.Tessaging.Teventive.TEventStore.Public.Exceptions;

class InvalidHistoryException(Guid taggregateId) : Exception($"TaggregateId: {taggregateId}");