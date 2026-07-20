using Compze.Tessaging.Endpoints.Discovery;

namespace Compze.Tessaging.Transport.Discovery;

interface IEndpointDiscoveryQueryTransport
{
   Task<TResult> GetAsync<TResult>(ITuery<TResult> query, EndpointAddress address, CancellationToken cancellationToken = default);
}
