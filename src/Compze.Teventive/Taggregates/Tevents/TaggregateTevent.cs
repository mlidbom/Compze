using Compze.Abstractions;
using Compze.Abstractions.Time;
using Compze.Tessaging;

namespace Compze.Teventive.Taggregates.Tevents;

public abstract class TaggregateTevent() : IMutableTaggregateTevent
{
    protected TaggregateTevent(TaggregateId taggregateId) : this() => TaggregateId = taggregateId;

    public int TaggregateVersion { get; private set; }
    public TaggregateId TaggregateId { get; private set; } = null!; //We are being sneaky here, it is actually never allowed to be visibly null, but the taggregate class needs it to be null at first, and guarantees that it never escapes from it while still null.
    public DateTime UtcTimeStamp { get; private set; } = UtcTimeSource.UtcNow;

    ///<summary>The tevent's class, with the version it gave its taggregate and the taggregate's id: what names it in a message,<br/>
    /// such as that of a failure to dispatch it.</summary>
    public override string ToString() => $"{GetType().FullName} (version {TaggregateVersion} of taggregate {TaggregateId})";

#pragma warning disable CA1033 // We do not want these methods as part of the public interface of TaggregateTevent.
    void IMutableTaggregateTevent.SetTaggregateIdInternal(TaggregateId taggregateId) => TaggregateId = taggregateId;
    void IMutableTaggregateTevent.SetTaggregateVersionInternal(int taggregateVersion) => TaggregateVersion = taggregateVersion;
    void IMutableTaggregateTevent.SetUtcTimeStampInternal(DateTime utcTimeStamp) => UtcTimeStamp = utcTimeStamp;
#pragma warning restore CA1033
}