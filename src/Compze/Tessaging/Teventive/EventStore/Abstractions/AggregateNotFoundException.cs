using System;

namespace Compze.Tessaging.Teventive.EventStore.Abstractions;

class AggregateNotFoundException(Guid aggregateId) :
   ArgumentOutOfRangeException($"Aggregate root with Id: {aggregateId} not found");