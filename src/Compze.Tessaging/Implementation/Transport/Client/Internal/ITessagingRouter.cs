using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Hosting.Public;

namespace Compze.Tessaging.Implementation.Transport.Client.Internal;

interface ITessagingRouter
{
    ///<summary>Connects to every endpoint the registry currently lists — plus the endpoint's own inbox — and keeps reconciling the<br/>
    /// connections with the registry's membership until delivery stops: an endpoint that appears is connected, one whose address<br/>
    /// disappears is disconnected (its undelivered tessages wait in the outbox's storage for its return), and one that reappears at a<br/>
    /// new address — addresses are per-instance, identity is the <see cref="EndpointId"/> — has its connection replaced, its<br/>
    /// undelivered backlog following it. The first reconciliation completes before this method returns, so startup sees the<br/>
    /// registry's current membership connected.</summary>
    Task StartMaintainingConnectionsAsync(IEndpointRegistry endpointRegistry, EndpointAddress ownInboxAddress);
    void Stop();
    void StartDelivery();
    void StopDelivery();
    ITessagingInboxConnection ConnectionToHandlerFor(IRemotableTommand tommand);
    ///<summary>The connections to every endpoint whose advertised tevent subscriptions match <paramref name="wrappedTevent"/>. Advertised subscriptions are wrapper<br/>
    /// types, so matching is against the wrapper — pure type assignability. Which delivery leg the tevent travels to a matched subscriber is not routing's concern:<br/>
    /// the published tevent's own type decides that (see <see cref="Compze.Abstractions.Tessaging.Public.ITeventPublisher"/>).</summary>
    IReadOnlyList<ITessagingInboxConnection> SubscriberConnectionsFor(IPublisherTevent<IRemotableTevent> wrappedTevent);
}
