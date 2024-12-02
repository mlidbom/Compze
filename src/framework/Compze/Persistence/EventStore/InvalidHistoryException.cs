using System;

namespace Compze.Persistence.EventStore;

public class InvalidHistoryException : Exception
{
   public InvalidHistoryException(Guid aggregateId):base($"AggregateId: {aggregateId}")
   {
   }
}