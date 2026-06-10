
namespace Compze.Abstractions.Hosting.Public;

public interface IEndpointRegistry
{
    IEnumerable<EndpointAddress> ServerEndpointAddresses { get; }
}