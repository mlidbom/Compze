using Compze.Core.Public;

namespace Compze.Core.Tessaging.Teventive.TeventStore.Public.Exceptions;

public class InvalidHistoryException(TaggregateId taggregateId) : Exception($"TaggregateId: {taggregateId}");