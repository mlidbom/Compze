using System;

namespace Compze.Persistence.EventStore;

class AggregateNotFoundException(Guid aggregateId) : 
   ArgumentOutOfRangeException($"Aggregate root with Id: {aggregateId} not found");