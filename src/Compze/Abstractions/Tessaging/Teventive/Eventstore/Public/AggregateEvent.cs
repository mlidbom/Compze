using System;

namespace Compze.Abstractions.Tessaging.Teventive.Eventstore.Public;

public abstract class AggregateEvent() : IMutableAggregateEvent
{
    protected AggregateEvent(Guid aggregateId) : this() => AggregateId = aggregateId;

    public Guid MessageId { get; private set; } = Guid.CreateVersion7();
    public int AggregateVersion { get; private set; }
    public Guid AggregateId { get; private set; }
    public DateTime UtcTimeStamp { get; private set; } = DateTime.UtcNow; //Todo: Should use time source.

#pragma warning disable CA1033 // We do not want these methods as part of the public interface of AggregateEvent.
    void IMutableAggregateEvent.SetAggregateIdInternal(Guid aggregateId) => AggregateId = aggregateId;
    void IMutableAggregateEvent.SetAggregateVersionInternal(int aggregateVersion) => AggregateVersion = aggregateVersion;
    void IMutableAggregateEvent.SetUtcTimeStampInternal(DateTime utcTimeStamp) => UtcTimeStamp = utcTimeStamp;
    void IMutableAggregateEvent.SetMessageIdInternal(Guid messageId) => MessageId = messageId;
#pragma warning restore CA1033
}