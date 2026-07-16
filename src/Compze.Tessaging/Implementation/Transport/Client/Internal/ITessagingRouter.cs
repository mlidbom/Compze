using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Hosting.Public;

namespace Compze.Tessaging.Implementation.Transport.Client.Internal;

interface ITessagingRouter
{
    ///<summary>Connects to every endpoint the registry currently lists — plus the endpoint's own inbox always, so an exactly-once<br/>
    /// tommand the endpoint sends that its own handlers serve routes like any other — and keeps reconciling the connections with<br/>
    /// the registry's membership until delivery stops: an endpoint that appears is connected, one whose address disappears is<br/>
    /// disconnected (its undelivered tessages wait in the outbox's storage for its return), and one that reappears at a new<br/>
    /// address — addresses are per-instance, identity is the <see cref="EndpointId"/> — has its connection replaced, its<br/>
    /// undelivered backlog following it. Reconciliation waits on the registry's change signal<br/>
    /// (<see cref="IEndpointRegistry.AwaitPossibleMembershipChange"/>), so membership changes propagate at signal latency. The<br/>
    /// first reconciliation completes before this method returns, so startup sees the registry's current membership connected.<br/>
    /// A null <paramref name="endpointRegistry"/> means the endpoint declared no discovery: it serves, converses in-process, and<br/>
    /// self-sends — the self-connection needs no discovery — but connects to no other endpoint.</summary>
    Task StartMaintainingConnectionsAsync(IEndpointRegistry? endpointRegistry, EndpointAddress ownInboxAddress);
    void Stop();
    void StartDelivery();
    void StopDelivery();
    ///<summary>The live connection to the endpoint whose current advertisement handles <paramref name="tommand"/>'s type —<br/>
    /// null when no connected endpoint does. Deliberately liveness-only: a tommand binds to its one specific receiver at<br/>
    /// send time, preferring the live handler and falling back to the sole remembered one<br/>
    /// (see <see cref="Peers.IPeerRegistry.HandlerIdsFor"/>), so a handler being down never makes the send explode.</summary>
    ITessagingInboxConnection? LiveConnectionToHandlerFor(IRemotableTommand tommand);
    ///<summary>The connections to every endpoint whose advertised tevent subscriptions match <paramref name="wrappedTevent"/>. Advertised subscriptions are wrapper<br/>
    /// types, so matching is against the wrapper — pure type assignability. Which delivery leg the tevent travels to a matched subscriber is not routing's concern:<br/>
    /// the published tevent's own type decides that (see <see cref="Compze.Abstractions.Tessaging.Public.IUnitOfWorkTeventPublisher"/>).</summary>
    IReadOnlyList<ITessagingInboxConnection> SubscriberConnectionsFor(IPublisherTevent<IRemotableTevent> wrappedTevent);
}
