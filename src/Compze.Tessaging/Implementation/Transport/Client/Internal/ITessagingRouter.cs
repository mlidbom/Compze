using System.Collections.Generic;
using System.Threading.Tasks;
using Compze.Core.Tessaging.Public;
using Compze.Core.Tessaging.Transport.Internal;

namespace Compze.Tessaging.Implementation.Transport.Client.Internal;

interface ITessagingRouter
{
    Task ConnectAsync(EndPointAddress remoteEndpointAddress);
    void Stop();
    void StartDelivery();
    void StopDelivery();
    ITessagingInboxConnection ConnectionToHandlerFor(IRemotableTommand tommand);
    IReadOnlyList<ITessagingInboxConnection> SubscriberConnectionsFor(IExactlyOnceTevent tevent);
}
