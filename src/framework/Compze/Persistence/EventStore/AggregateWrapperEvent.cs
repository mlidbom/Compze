using System;
using Compze.Tessaging;

namespace Compze.Persistence.EventStore;

public abstract class AggregateWrapperEvent<TBaseEventInterface>(TBaseEventInterface @event) : WrapperEvent<TBaseEventInterface>(@event), IAggregateWrapperEvent<TBaseEventInterface>
   where TBaseEventInterface : IAggregateEvent;

public interface IMutableAggregateEvent : IAggregateEvent
{
   [Obsolete(ObsoleteMessage.ForInternalUseOnly)] void SetAggregateIdInternal(Guid aggregateId);
   [Obsolete(ObsoleteMessage.ForInternalUseOnly)] void SetAggregateVersionInternal(int aggregateVersion);
   [Obsolete(ObsoleteMessage.ForInternalUseOnly)] void SetUtcTimeStampInternal(DateTime utcTimeStamp);
   [Obsolete(ObsoleteMessage.ForInternalUseOnly)] void SetMessageIdInternal(Guid messageId);
}

public abstract class AggregateEvent() : IMutableAggregateEvent
{
   protected AggregateEvent(Guid aggregateId) : this() => AggregateId = aggregateId;

   public Guid MessageId { get; private set; } = Guid.NewGuid();
   public int AggregateVersion { get; private set; }
   public Guid AggregateId { get; private set; }
   public DateTime UtcTimeStamp { get; private set; } = DateTime.UtcNow; //Todo:bug: Should use timesource.

#pragma warning disable CA1033 // We do not want these methods as part of the public interface of AggregateEvent.
   void IMutableAggregateEvent.SetAggregateIdInternal(Guid aggregateId) => AggregateId = aggregateId;
   void IMutableAggregateEvent.SetAggregateVersionInternal(int aggregateVersion) => AggregateVersion = aggregateVersion;
   void IMutableAggregateEvent.SetUtcTimeStampInternal(DateTime utcTimeStamp) => UtcTimeStamp = utcTimeStamp;
   void IMutableAggregateEvent.SetMessageIdInternal(Guid messageId) => MessageId = messageId;
#pragma warning restore CA1033
}
