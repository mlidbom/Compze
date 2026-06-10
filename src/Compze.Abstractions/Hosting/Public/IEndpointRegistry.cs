
namespace Compze.Abstractions.Hosting.Public;

///<summary>
/// Knows the addresses of the server endpoints a sending endpoint should connect to. Registered in each
/// endpoint's container by the hosting layer: the testing host supplies one listing its own endpoints' inbox
/// addresses, while production endpoints fall back to reading addresses from application configuration.
///
/// Today its only consumer is Tessaging routing (the router connects to every address listed); whether the
/// concept stays paradigm-neutral or moves to Tessaging is an open design question.
///</summary>
public interface IEndpointRegistry
{
    IEnumerable<EndpointAddress> ServerEndpointAddresses { get; }
}
