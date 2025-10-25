using System;

namespace Compze.Abstractions.Tessaging.Teventive.Public.Taggregates.Tevents.Public;

public abstract class TaggregateTevent() : IMutableTaggregateTevent
{
    protected TaggregateTevent(Guid taggregateId) : this() => TaggregateId = taggregateId;

    public Guid TessageId { get; private set; } = Guid.CreateVersion7();
    public int TaggregateVersion { get; private set; }
    public Guid TaggregateId { get; private set; }
    public DateTime UtcTimeStamp { get; private set; } = DateTime.UtcNow; //Todo: Should use time source.

#pragma warning disable CA1033 // We do not want these methods as part of the public interface of TaggregateTevent.
    void IMutableTaggregateTevent.SetTaggregateIdInternal(Guid taggregateId) => TaggregateId = taggregateId;
    void IMutableTaggregateTevent.SetTaggregateVersionInternal(int taggregateVersion) => TaggregateVersion = taggregateVersion;
    void IMutableTaggregateTevent.SetUtcTimeStampInternal(DateTime utcTimeStamp) => UtcTimeStamp = utcTimeStamp;
    void IMutableTaggregateTevent.SetTessageIdInternal(Guid tessageId) => TessageId = tessageId;
#pragma warning restore CA1033
}