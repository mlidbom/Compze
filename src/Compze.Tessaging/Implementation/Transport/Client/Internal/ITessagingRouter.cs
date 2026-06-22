using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Hosting.Public;

namespace Compze.Tessaging.Implementation.Transport.Client.Internal;

interface ITessagingRouter
{
    Task ConnectAsync(EndpointAddress remoteEndpointAddress);
    void Stop();
    void StartDelivery();
    void StopDelivery();
    ITessagingInboxConnection ConnectionToHandlerFor(IRemotableTommand tommand);
    IReadOnlyList<ITessagingInboxConnection> SubscriberConnectionsFor(IExactlyOnceTevent tevent);
}
