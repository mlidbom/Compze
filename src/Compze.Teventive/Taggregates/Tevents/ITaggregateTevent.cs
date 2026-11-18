using Compze.Abstractions;
using Compze.Tessaging.TessageTypes;

namespace Compze.Teventive.Taggregates.Tevents;

public interface ITaggregateTevent<out TTeventInterface> : IPublisherTevent<TTeventInterface>
   where TTeventInterface : ITaggregateTevent;

///<summary>A tevent a taggregate publishes: it carries the taggregate's id, the version the tevent gave it, and when it was published.</summary>
///<remarks>It declares no delivery guarantee and no identity of its own, so it is published and handled synchronously within the process.<br/>
/// A hierarchy that a tevent store keeps declares <see cref="ITaggregateTeventWithIdentity"/>, and one that is delivered to other<br/>
/// processes declares its guarantee, such as <see cref="IExactlyOnceTevent"/>.</remarks>
public interface ITaggregateTevent : ITevent
{
   ///<summary>The version this tevent gave its taggregate: see <see cref="ITaggregate.Version"/>.</summary>
   ///<remarks>In a tevent store's history it counts the taggregate's tevents, so with <see cref="TaggregateId"/> it is unique. A taggregate<br/>
   /// that a memory image state store keeps counts from 1 again at each load, so across loads two of its tevents can carry the same.</remarks>
   int TaggregateVersion { get; }
   TaggregateId TaggregateId { get; }
   //Todo:Consider using DateTimeOffset instead of DateTime for the timestamp in tevents. DateTime is fragile and requires every bit of code that deals with it in composable to remember to translate dates to UTC. Even if it does comparison of datetimes is incorrect if we ever compare with a  non-utc value. All of these problems disappear with DateTimeOffset.
   DateTime UtcTimeStamp { get; }
}