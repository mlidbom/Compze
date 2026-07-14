using Compze.Abstractions.Public;
using Compze.Tessaging.Teventive.TeventStore.Public.Exceptions;
using Compze.Teventive.Taggregates.Tevents.Public;

// ReSharper disable LoopCanBeConvertedToTuery

namespace Compze.Tessaging.Teventive.TeventStore;

static class TaggregateHistoryValidator
{
   public static void ValidateHistory(TaggregateId taggregateId, IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> history)
   {
      var version = 1;
      foreach(var wrappedTevent in history)
      {
         if(wrappedTevent.Tevent.TaggregateVersion != version++)
         {
            throw new InvalidHistoryException(taggregateId);
         }
      }
   }
}