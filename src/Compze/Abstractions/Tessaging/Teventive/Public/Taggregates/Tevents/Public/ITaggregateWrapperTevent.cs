using System;
using Compze.Core.Tessaging.Public;

namespace Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;

///<summary>
/// When different taggregates publish tevents of the same type it is impossible to distinguish the publisher by that tevent alone.
/// To ensure any tevent can be subscribed to, taggregate automatically wrap their tevents in tevents of this type.
///
/// * For example when taggregates inherit each other, or uses a reusable** tomponent or tentity.
/// ** Not exclusive to that taggregate
/// </summary>
public interface ITaggregateTypeIdentifyingTevent<out TTeventInterface> : IExactlyOncePublisherTypeIdentifyingTevent<TTeventInterface>
   where TTeventInterface : ITaggregateTevent;

public interface ITaggregateTevent : IExactlyOnceTevent
{
   int TaggregateVersion { get; }
   //Refactor: We should use a custom type for TaggregateIds. Likely a record struct.
   Guid TaggregateId { get; }
   //Todo:Consider using DateTimeOffset instead of DateTime for the timestamp in tevents. DateTime is fragile and requires every bit of code that deals with it in composable to remember to translate dates to UTC. Even if it does comparison of datetimes is incorrect if we ever compare with a  non-utc value. All of these problems disappear with DateTimeOffset.
   DateTime UtcTimeStamp { get; }
}