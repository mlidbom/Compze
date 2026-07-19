using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Hosting.Public;

namespace Compze.Tessaging.Internals.Transport;

interface IEndpointDiscoveryQueryTransport
{
   Task<TResult> GetAsync<TResult>(ITuery<TResult> query, EndpointAddress address, CancellationToken cancellationToken = default);
}
