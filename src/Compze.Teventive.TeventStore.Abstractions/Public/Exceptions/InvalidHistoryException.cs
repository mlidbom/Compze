using Compze.Abstractions.Public;

namespace Compze.Teventive.TeventStore.Abstractions.Public.Exceptions;

public class InvalidHistoryException(TaggregateId taggregateId) : Exception($"TaggregateId: {taggregateId}");