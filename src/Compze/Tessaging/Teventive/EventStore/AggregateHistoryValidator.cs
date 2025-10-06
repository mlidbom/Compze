using System;
using System.Collections.Generic;
using Compze.Tessaging.Teventive.EventStore.Abstractions;

// ReSharper disable LoopCanBeConvertedToQuery

namespace Compze.Tessaging.Teventive.EventStore;

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