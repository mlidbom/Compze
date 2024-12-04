using System;
using Compze.Messaging;

namespace Compze.Persistence.EventStore;

public abstract class AggregateEvent<TBaseEventInterface>(TBaseEventInterface @event) : WrapperEvent<TBaseEventInterface>(@event), IAggregateEvent<TBaseEventInterface>
   where TBaseEventInterface : IAggregateEvent;

public interface IMutableAggregateEvent : IAggregateEvent
{
   void SetAggregateId(Guid aggregateId);
   void SetAggregateVersion(int aggregateVersion);
   void SetUtcTimeStamp(DateTime utcTimeStamp);
   void SetMessageId(Guid messageId);
}

public abstract class AggregateEvent() : IMutableAggregateEvent
{
   protected AggregateEvent(Guid aggregateId) : this() => AggregateId = aggregateId;

   public Guid MessageId { get; private set; } = Guid.NewGuid();
   public int AggregateVersion { get; private set; }
   public Guid AggregateId { get; private set; }
   public DateTime UtcTimeStamp { get; private set; } = DateTime.UtcNow; //Todo:bug: Should use timesource.

#pragma warning disable CA1033 // We do not want these methods as part of the public interface of AggregateEvent.
   void IMutableAggregateEvent.SetAggregateId(Guid aggregateId) => AggregateId = aggregateId;
   void IMutableAggregateEvent.SetAggregateVersion(int aggregateVersion) => AggregateVersion = aggregateVersion;
   void IMutableAggregateEvent.SetUtcTimeStamp(DateTime utcTimeStamp) => UtcTimeStamp = utcTimeStamp;
   void IMutableAggregateEvent.SetMessageId(Guid messageId) => MessageId = messageId;
#pragma warning restore CA1033
}
