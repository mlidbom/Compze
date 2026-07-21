using Compze.Abstractions;

namespace Compze.Teventive.TeventStore.Abstractions.Exceptions;

public class TaggregateNotFoundException(TaggregateId taggregateId) :
   ArgumentOutOfRangeException($"Taggregate root with Id: {taggregateId} not found");