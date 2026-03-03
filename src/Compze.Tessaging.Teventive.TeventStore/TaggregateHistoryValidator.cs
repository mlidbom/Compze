using Compze.Core.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Core.Tessaging.Teventive.TeventStore.Public.Exceptions;

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