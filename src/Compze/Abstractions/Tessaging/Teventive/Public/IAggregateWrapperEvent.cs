using System;
using Compze.Abstractions.Tessaging.Public;

namespace Compze.Abstractions.Tessaging.Teventive.Public;

public interface IAggregateWrapperTevent<out TEventInterface> : IExactlyOnceWrapperTevent<TEventInterface>
   where TEventInterface : IAggregateTevent;

public interface IAggregateTevent : IExactlyOnceTevent
{
   int AggregateVersion { get; }
   //Refactor: We should use a custom type for AggregateIds. Likely a record struct.
   Guid AggregateId { get; }
   //Todo:Consider using DateTimeOffset instead of DateTime for the timestamp in events. DateTime is fragile and requires every bit of code that deals with it in composable to remember to translate dates to UTC. Even if it does comparison of datetimes is incorrect if we ever compare with a  non-utc value. All of these problems disappear with DateTimeOffset.
   DateTime UtcTimeStamp { get; }
}