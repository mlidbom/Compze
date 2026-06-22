using Compze.Abstractions.Public;
using Compze.Abstractions.Time.Public;

namespace Compze.Teventive.Public.Taggregates.Tevents.Public;

public abstract class TaggregateTevent() : IMutableTaggregateTevent
{
    protected TaggregateTevent(TaggregateId taggregateId) : this() => TaggregateId = taggregateId;

    public TessageId Id { get; private set; } = new();
    public int TaggregateVersion { get; private set; }
    public TaggregateId TaggregateId { get; private set; } = null!; //We are being sneaky here, it is actually never allowed to be visibly null, but the taggregate class needs it to be null at first, and guarantees that it never escapes from it while still null.
    public DateTime UtcTimeStamp { get; private set; } = UtcTimeSource.UtcNow;

#pragma warning disable CA1033 // We do not want these methods as part of the public interface of TaggregateTevent.
    void IMutableTaggregateTevent.SetTaggregateIdInternal(TaggregateId taggregateId) => TaggregateId = taggregateId;
    void IMutableTaggregateTevent.SetTaggregateVersionInternal(int taggregateVersion) => TaggregateVersion = taggregateVersion;
    void IMutableTaggregateTevent.SetUtcTimeStampInternal(DateTime utcTimeStamp) => UtcTimeStamp = utcTimeStamp;
    void IMutableTaggregateTevent.SetTessageIdInternal(TessageId tessageId) => Id = tessageId;
#pragma warning restore CA1033
}