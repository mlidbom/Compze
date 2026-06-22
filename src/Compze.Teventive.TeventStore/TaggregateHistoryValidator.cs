using Compze.Abstractions.Public;
using Compze.Tessaging.Teventive.TeventStore.Public.Exceptions;
using Compze.Teventive.Public.Taggregates.Tevents.Public;

// ReSharper disable LoopCanBeConvertedToTuery

namespace Compze.Tessaging.Teventive.TeventStore;

static class TaggregateHistoryValidator
{
   public static void ValidateHistory(TaggregateId taggregateId, IReadOnlyList<ITaggregateTevent> history)
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