using Compze.Abstractions;

namespace Compze.Teventive.TeventStore.Abstractions.Exceptions;

public class InvalidHistoryException(TaggregateId taggregateId) : Exception($"TaggregateId: {taggregateId}");