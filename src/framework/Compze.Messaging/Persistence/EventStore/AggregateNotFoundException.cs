using System;

namespace Compze.Persistence.EventStore;

public class AggregateNotFoundException : Exception
{
   public AggregateNotFoundException(Guid aggregateId): base($"Aggregate root with Id: {aggregateId} not found")
   {

   }
}