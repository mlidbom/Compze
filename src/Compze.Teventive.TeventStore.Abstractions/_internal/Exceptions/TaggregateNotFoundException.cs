using Compze.Abstractions;

namespace Compze.Teventive.TeventStore.Abstractions._internal.Exceptions;

class TaggregateNotFoundException(TaggregateId taggregateId) :
   ArgumentOutOfRangeException($"Taggregate root with Id: {taggregateId} not found");