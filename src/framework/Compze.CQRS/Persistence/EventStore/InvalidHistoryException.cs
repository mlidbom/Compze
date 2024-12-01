using System;

namespace Compze.Persistence.EventStore;

class InvalidHistoryException : Exception
{
   public InvalidHistoryException(Guid aggregateId):base($"AggregateId: {aggregateId}")
   {
   }
}