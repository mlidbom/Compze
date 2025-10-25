using System;
using System.Collections.Generic;
using Compze.Abstractions.Tessaging.Teventive.TeventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.TeventStore.Public.Exceptions;
using Compze.Abstractions.Tessaging.Teventive.Public;

// ReSharper disable LoopCanBeConvertedToTuery

namespace Compze.Tessaging.Teventive.TeventStore;

static class AggregateHistoryValidator
{
   public static void ValidateHistory(Guid aggregateId, IReadOnlyList<IAggregateTevent> history)
   {
      var version = 1;
      foreach(var aggregateTevent in history)
      {
         if(aggregateTevent.AggregateVersion != version++)
         {
            throw new InvalidHistoryException(aggregateId);
         }
      }
   }
}