using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Hosting.Public;

namespace Compze.Internals.Transport;

public interface IEndpointDiscoveryQueryTransport
{
   Task<TResult> GetAsync<TResult>(IQuery<TResult> query, EndpointAddress address);
}
