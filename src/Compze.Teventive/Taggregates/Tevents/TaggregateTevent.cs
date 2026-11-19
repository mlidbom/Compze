using Compze.Abstractions;
using Compze.Abstractions.Time;
using Compze.Tessaging;
using Compze.Teventive.Taggregates.Tevents._internal;

namespace Compze.Teventive.Taggregates.Tevents;

public abstract class TaggregateTevent() : IMutableTaggregateTevent
{
    protected TaggregateTevent(TaggregateId taggregateId) : this() => TaggregateId = taggregateId;

    public int TaggregateVersion { get; private set; }
    public TaggregateId TaggregateId { get; private set; } = null!; //We are being sneaky here, it is actually never allowed to be visibly null, but the taggregate class needs it to be null at first, and guarantees that it never escapes from it while still null.
    public DateTime UtcTimeStamp { get; private set; } = UtcTimeSource.UtcNow;

    void IMutableTaggregateTevent.SetTaggregateIdInternal(TaggregateId taggregateId) => TaggregateId = taggregateId;
    void IMutableTaggregateTevent.SetTaggregateVersionInternal(int taggregateVersion) => TaggregateVersion = taggregateVersion;
    void IMutableTaggregateTevent.SetUtcTimeStampInternal(DateTime utcTimeStamp) => UtcTimeStamp = utcTimeStamp;
}