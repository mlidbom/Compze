using System;

namespace Compze.Abstractions.Tessaging.Teventive.TeventStore.Public.Exceptions;

class TaggregateNotFoundException(Guid taggregateId) :
   ArgumentOutOfRangeException($"Taggregate root with Id: {taggregateId} not found");