using System;
using System.Collections.Generic;
using Compze.Abstractions.Tessaging.Teventive.EventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.EventStore.Public.Exceptions;
using Compze.Abstractions.Tessaging.Teventive.Public;

// ReSharper disable LoopCanBeConvertedToQuery

namespace Compze.Tessaging.Teventive.EventStore;

static class AggregateHistoryValidator
{
   public static void ValidateHistory(Guid aggregateId, IReadOnlyList<IAggregateTevent> history)
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