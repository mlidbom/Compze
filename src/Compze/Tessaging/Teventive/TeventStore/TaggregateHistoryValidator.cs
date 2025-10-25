using System;
using System.Collections.Generic;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Abstractions.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Abstractions.Tessaging.Teventive.TEventStore.Public.Exceptions;

// ReSharper disable LoopCanBeConvertedToTuery

namespace Compze.Tessaging.Teventive.TeventStore;

static class TaggregateHistoryValidator
{
   public static void ValidateHistory(Guid taggregateId, IReadOnlyList<ITaggregateTevent> history)
   {
      var version = 1;
      foreach(var taggregateTevent in history)
      {
         if(taggregateTevent.TaggregateVersion != version++)
         {
            throw new InvalidHistoryException(taggregateId);
         }
      }
   }
}