using System;
using Compze.Core.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;

namespace Compze.Core.Tessaging.Teventive.Public;

public interface IMutableTaggregateTevent : ITaggregateTevent
{
    [Obsolete(ObsoleteMessage.ForInternalUseOnly)] void SetTaggregateIdInternal(Guid taggregateId);
    [Obsolete(ObsoleteMessage.ForInternalUseOnly)] void SetTaggregateVersionInternal(int taggregateVersion);
    [Obsolete(ObsoleteMessage.ForInternalUseOnly)] void SetUtcTimeStampInternal(DateTime utcTimeStamp);
    [Obsolete(ObsoleteMessage.ForInternalUseOnly)] void SetTessageIdInternal(Guid tessageId);
}