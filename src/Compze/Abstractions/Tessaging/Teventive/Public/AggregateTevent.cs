using System;

namespace Compze.Abstractions.Tessaging.Teventive.Public;

public abstract class AggregateTevent() : IMutableAggregateTevent
{
    protected AggregateTevent(Guid aggregateId) : this() => AggregateId = aggregateId;

    public Guid TessageId { get; private set; } = Guid.CreateVersion7();
    public int AggregateVersion { get; private set; }
    public Guid AggregateId { get; private set; }
    public DateTime UtcTimeStamp { get; private set; } = DateTime.UtcNow; //Todo: Should use time source.

#pragma warning disable CA1033 // We do not want these methods as part of the public interface of AggregateTevent.
    void IMutableAggregateTevent.SetAggregateIdInternal(Guid aggregateId) => AggregateId = aggregateId;
    void IMutableAggregateTevent.SetAggregateVersionInternal(int aggregateVersion) => AggregateVersion = aggregateVersion;
    void IMutableAggregateTevent.SetUtcTimeStampInternal(DateTime utcTimeStamp) => UtcTimeStamp = utcTimeStamp;
    void IMutableAggregateTevent.SetTessageIdInternal(Guid tessageId) => TessageId = tessageId;
#pragma warning restore CA1033
}