using System;

namespace Compze.Core.Tessaging.Teventive.TEventStore.Public.Exceptions;

class TaggregateNotFoundException(Guid taggregateId) :
   ArgumentOutOfRangeException($"Taggregate root with Id: {taggregateId} not found");