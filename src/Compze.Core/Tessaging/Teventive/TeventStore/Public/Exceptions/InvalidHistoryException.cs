using Compze.Abstractions.Public;

namespace Compze.Core.Tessaging.Teventive.TeventStore.Public.Exceptions;

public class InvalidHistoryException(TaggregateId taggregateId) : Exception($"TaggregateId: {taggregateId}");