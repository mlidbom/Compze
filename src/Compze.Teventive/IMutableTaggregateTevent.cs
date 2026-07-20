using Compze.Abstractions.Public;
using Compze.Tessaging;
using Compze.Teventive.Taggregates.Tevents.Public;

namespace Compze.Teventive;

public interface IMutableTaggregateTevent : ITaggregateTevent
{
    [Obsolete(ObsoleteMessage.ForInternalUseOnly)] void SetTaggregateIdInternal(TaggregateId taggregateId);
    [Obsolete(ObsoleteMessage.ForInternalUseOnly)] void SetTaggregateVersionInternal(int taggregateVersion);
    [Obsolete(ObsoleteMessage.ForInternalUseOnly)] void SetUtcTimeStampInternal(DateTime utcTimeStamp);
    [Obsolete(ObsoleteMessage.ForInternalUseOnly)] void SetTessageIdInternal(TessageId tessageId);
}