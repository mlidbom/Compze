using System;

namespace Compze.Abstractions.Internal.Persistence.EventStore;

class AggregateNotFoundException(Guid aggregateId) :
   ArgumentOutOfRangeException($"Aggregate root with Id: {aggregateId} not found");