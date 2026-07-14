using Compze.Abstractions.Public;
using Compze.Teventive.Taggregates.Tevents.Public;
using Compze.Teventive.TeventStore.Abstractions.Public.Exceptions;

// ReSharper disable LoopCanBeConvertedToTuery

namespace Compze.Teventive.TeventStore;

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