using System;
using Compze.Abstractions.Public;

namespace Compze.Abstractions.Tessaging.Teventive.Public;

public interface IMutableAggregateTevent : IAggregateTevent
{
    [Obsolete(ObsoleteMessage.ForInternalUseOnly)] void SetAggregateIdInternal(Guid aggregateId);
    [Obsolete(ObsoleteMessage.ForInternalUseOnly)] void SetAggregateVersionInternal(int aggregateVersion);
    [Obsolete(ObsoleteMessage.ForInternalUseOnly)] void SetUtcTimeStampInternal(DateTime utcTimeStamp);
    [Obsolete(ObsoleteMessage.ForInternalUseOnly)] void SetTessageIdInternal(Guid tessageId);
}