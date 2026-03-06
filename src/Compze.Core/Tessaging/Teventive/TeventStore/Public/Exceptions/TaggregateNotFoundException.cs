using Compze.Abstractions.Public;

namespace Compze.Core.Tessaging.Teventive.TeventStore.Public.Exceptions;

public class TaggregateNotFoundException(TaggregateId taggregateId) :
   ArgumentOutOfRangeException($"Taggregate root with Id: {taggregateId} not found");