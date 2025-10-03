using Compze.EventStore.Abstractions;
using System;
using System.Collections.Generic;
using Compze.Abstractions.Internal.Persistence.EventStore;

// ReSharper disable LoopCanBeConvertedToQuery

namespace Compze.EventStore;

static class AggregateHistoryValidator
{
   public static void ValidateHistory(Guid aggregateId, IReadOnlyList<IAggregateEvent> history)
   {
      var version = 1;
      foreach(var aggregateEvent in history)
      {
         if(aggregateEvent.AggregateVersion != version++)
         {
            throw new InvalidHistoryException(aggregateId);
         }
      }
   }
}