using System.Collections.Generic;
using Compze.Core.Tessaging.Public;

namespace Compze.Tessaging.Implementation.Transport.Client.Internal;

public interface IExactlyOnceRoutingClient
{
    IExactlyOnceInboxConnection ConnectionToHandlerFor(IRemotableTommand tommand);
    IReadOnlyList<IExactlyOnceInboxConnection> SubscriberConnectionsFor(IExactlyOnceTevent tevent);
}
