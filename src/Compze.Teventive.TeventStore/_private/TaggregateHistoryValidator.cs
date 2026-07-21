using Compze.Abstractions;
using Compze.Teventive.Taggregates.Tevents;
using Compze.Teventive.TeventStore.Abstractions.Exceptions;

// ReSharper disable LoopCanBeConvertedToTuery

namespace Compze.Teventive.TeventStore._private;

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