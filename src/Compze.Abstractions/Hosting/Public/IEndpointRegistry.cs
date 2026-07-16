
namespace Compze.Abstractions.Hosting.Public;

///<summary>
/// Knows the addresses of the server endpoints a sending endpoint should connect to — the read side of endpoint
/// discovery, whose write side is <see cref="IEndpointAddressAnnouncer"/>. An endpoint declares the registry it
/// discovers through on a distributed communication style's feature (<c>DiscoverEndpointsThrough(...)</c> on
/// distributed Tessaging — or through <c>AddExactlyOnceTessaging()</c>, which delegates — and on distributed
/// Typermedia; or <c>ParticipateIn(...)</c> for an <see cref="IEndpointRegistryAndAnnouncer"/>): a same-machine
/// suite participates in the shared interprocess registry, and the testing host runs every test's endpoints on
/// one of its own. Declaring none means the endpoint only serves that style — nothing is discovered, so its
/// router connects to no other endpoint.
///
/// Both routers reconcile their connections against the declared registry's live membership; each waits between
/// reconciliation passes in <see cref="AwaitPossibleMembershipChange"/>.
///</summary>
public interface IEndpointRegistry
{
    IEnumerable<EndpointAddress> ServerEndpointAddresses { get; }

    ///<summary>Blocks until the registry's membership may have changed, <paramref name="timeout"/> elapses, or<br/>
    /// <paramref name="cancellationToken"/> is cancelled — whichever comes first. A router waits here between reconciliation<br/>
    /// passes: a registry that can observe its own changes (the interprocess registry — every announcement and retraction<br/>
    /// raises a cross-process signal) wakes its waiters at signal latency, so topology changes propagate nearly instantly<br/>
    /// instead of at the waiting interval. The default implementation just waits out the timeout — exactly right for a<br/>
    /// registry whose membership never changes on its own initiative (a fixed address list) or that cannot signal.<br/>
    /// The timeout is not merely a fallback: no signal can announce a crashed process (it signals nothing — its addresses<br/>
    /// just stop being listed), and a failed connection needs a retry that no membership change will trigger, so the<br/>
    /// periodic pass the timeout drives must exist regardless.</summary>
    void AwaitPossibleMembershipChange(TimeSpan timeout, CancellationToken cancellationToken) => cancellationToken.WaitHandle.WaitOne(timeout);
}
