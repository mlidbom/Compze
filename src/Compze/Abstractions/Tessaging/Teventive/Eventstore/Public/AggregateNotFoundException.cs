using System;

namespace Compze.Abstractions.Tessaging.Teventive.EventStore.Public;

class AggregateNotFoundException(Guid aggregateId) :
   ArgumentOutOfRangeException($"Aggregate root with Id: {aggregateId} not found");