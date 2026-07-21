using Compze.Abstractions;

namespace Compze.Teventive.TeventStore.Abstractions._internal.Exceptions;

class InvalidHistoryException(TaggregateId taggregateId) : Exception($"TaggregateId: {taggregateId}");