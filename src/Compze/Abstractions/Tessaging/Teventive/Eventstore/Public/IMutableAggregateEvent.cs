using System;
using Compze.Abstractions;
using Compze.Abstractions.Public;

namespace Compze.Tessaging.Teventive.EventStore.Abstractions;

public interface IMutableAggregateEvent : IAggregateEvent
{
    [Obsolete(ObsoleteMessage.ForInternalUseOnly)] void SetAggregateIdInternal(Guid aggregateId);
    [Obsolete(ObsoleteMessage.ForInternalUseOnly)] void SetAggregateVersionInternal(int aggregateVersion);
    [Obsolete(ObsoleteMessage.ForInternalUseOnly)] void SetUtcTimeStampInternal(DateTime utcTimeStamp);
    [Obsolete(ObsoleteMessage.ForInternalUseOnly)] void SetMessageIdInternal(Guid messageId);
}