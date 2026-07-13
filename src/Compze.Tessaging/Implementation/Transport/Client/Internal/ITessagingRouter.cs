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
    ///<summary>The connections to every endpoint whose advertised tevent subscriptions match <paramref name="wrappedTevent"/>. Advertised subscriptions are wrapper types, so matching is against the wrapper.</summary>
    IReadOnlyList<ITessagingInboxConnection> SubscriberConnectionsFor(IPublisherIdentifyingTevent<IExactlyOnceTevent> wrappedTevent);
}
