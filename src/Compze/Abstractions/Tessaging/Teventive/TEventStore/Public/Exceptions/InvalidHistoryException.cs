using System;

namespace Compze.Abstractions.Tessaging.Teventive.TeventStore.Public.Exceptions;

class InvalidHistoryException(Guid taggregateId) : Exception($"TaggregateId: {taggregateId}");