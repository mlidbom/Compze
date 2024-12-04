using System;
using Compze.Messaging;

namespace Compze.Persistence.EventStore;

public interface IAggregateEvent<out TEventInterface> : IExactlyOnceWrapperEvent<TEventInterface>
   where TEventInterface : IAggregateEvent;

public interface IAggregateEvent : IExactlyOnceEvent
{
   int AggregateVersion { get; }
   //Refactor: We should use a custom type for AggregateIds. Likely a record struct.
   Guid AggregateId { get; }
   //Todo:Consider using DateTimeOffset instead of DateTime for the timestamp in events. DateTime is fragile and requires every bit of code that deals with it in composable to remember to translate dates to UTC. Even if it does comparison of datetimes is incorrect if we ever compare with a  non-utc value. All of these problems disappear with DateTimeOffset.
   DateTime UtcTimeStamp { get; }
}