using System;

namespace Compze.EventStore.Abstractions;

class AggregateNotFoundException(Guid aggregateId) :
   ArgumentOutOfRangeException($"Aggregate root with Id: {aggregateId} not found");