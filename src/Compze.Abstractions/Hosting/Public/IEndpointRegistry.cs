
namespace Compze.Abstractions.Hosting.Public;

///<summary>
/// Knows the addresses of the server endpoints a sending endpoint should connect to — the read side of endpoint
/// discovery, whose write side is <see cref="IEndpointAddressAnnouncer"/>. An endpoint declares the registry it
/// discovers through on its distributed-Tessaging feature (<c>AddDistributedTessaging().DiscoverEndpointsThrough(...)</c>,
/// or <c>ParticipateIn(...)</c> for a registry that is also the announcer it announces to): the testing host declares
/// one listing its own endpoints' addresses, a same-machine suite declares the shared interprocess registry, and an
/// endpoint declaring none falls back to reading addresses from application configuration.
///
/// Today its only consumer is Tessaging routing (the router connects to every address listed); whether the
/// concept stays neutral or moves to Tessaging is an open design question.
///</summary>
public interface IEndpointRegistry
{
    IEnumerable<EndpointAddress> ServerEndpointAddresses { get; }
}
