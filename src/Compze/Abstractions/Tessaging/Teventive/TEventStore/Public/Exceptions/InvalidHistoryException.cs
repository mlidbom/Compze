using System;

namespace Compze.Core.Tessaging.Teventive.TeventStore.Public.Exceptions;

class InvalidHistoryException(Guid taggregateId) : Exception($"TaggregateId: {taggregateId}");