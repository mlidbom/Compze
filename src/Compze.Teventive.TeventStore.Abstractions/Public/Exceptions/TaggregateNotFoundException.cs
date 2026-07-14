using Compze.Abstractions.Public;

namespace Compze.Teventive.TeventStore.Abstractions.Public.Exceptions;

public class TaggregateNotFoundException(TaggregateId taggregateId) :
   ArgumentOutOfRangeException($"Taggregate root with Id: {taggregateId} not found");