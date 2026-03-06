using Compze.Abstractions.Public;
using Compze.Abstractions.Tessaging.Public;

namespace Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;

public interface ITaggregateIdentifyingTevent<out TTeventInterface> : IExactlyOncePublisherIdentifyingTevent<TTeventInterface>
   where TTeventInterface : ITaggregateTevent;

public interface ITaggregateTevent : IExactlyOnceTevent
{
   int TaggregateVersion { get; }
   TaggregateId TaggregateId { get; }
   //Todo:Consider using DateTimeOffset instead of DateTime for the timestamp in tevents. DateTime is fragile and requires every bit of code that deals with it in composable to remember to translate dates to UTC. Even if it does comparison of datetimes is incorrect if we ever compare with a  non-utc value. All of these problems disappear with DateTimeOffset.
   DateTime UtcTimeStamp { get; }
}