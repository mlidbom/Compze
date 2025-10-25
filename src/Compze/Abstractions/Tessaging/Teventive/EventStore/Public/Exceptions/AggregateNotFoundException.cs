using System;

namespace Compze.Abstractions.Tessaging.Teventive.TeventStore.Public.Exceptions;

class AggregateNotFoundException(Guid aggregateId) :
   ArgumentOutOfRangeException($"Aggregate root with Id: {aggregateId} not found");