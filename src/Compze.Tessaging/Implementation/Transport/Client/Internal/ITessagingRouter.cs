using System.Collections.Generic;
using Compze.Core.Tessaging.Public;

namespace Compze.Tessaging.Implementation.Transport.Client.Internal;

public interface ITessagingRouter
{
    ITessagingInboxConnection ConnectionToHandlerFor(IRemotableTommand tommand);
    IReadOnlyList<ITessagingInboxConnection> SubscriberConnectionsFor(IExactlyOnceTevent tevent);
}
