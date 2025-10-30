using System;
using Compze.Core.Public;

namespace Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;

public abstract class TaggregateTevent() : IMutableTaggregateTevent
{
    protected TaggregateTevent(TaggregateId taggregateId) : this() => TaggregateId = taggregateId;

    public TessageId Id { get; private set; } = new TessageId();
    public int TaggregateVersion { get; private set; }
    public TaggregateId TaggregateId { get; private set; }
    public DateTime UtcTimeStamp { get; private set; } = DateTime.UtcNow; //Todo: Should use time source.

#pragma warning disable CA1033 // We do not want these methods as part of the public interface of TaggregateTevent.
    void IMutableTaggregateTevent.SetTaggregateIdInternal(TaggregateId taggregateId) => TaggregateId = taggregateId;
    void IMutableTaggregateTevent.SetTaggregateVersionInternal(int taggregateVersion) => TaggregateVersion = taggregateVersion;
    void IMutableTaggregateTevent.SetUtcTimeStampInternal(DateTime utcTimeStamp) => UtcTimeStamp = utcTimeStamp;
    void IMutableTaggregateTevent.SetTessageIdInternal(TessageId tessageId) => Id = tessageId;
#pragma warning restore CA1033
}