using Compze.Tessaging.Abstractions.TessageTypes;
using Compze.Tessaging.Endpoints.Discovery;

namespace Compze.Tessaging.Internals.Transport;

interface IEndpointDiscoveryQueryTransport
{
   Task<TResult> GetAsync<TResult>(ITuery<TResult> query, EndpointAddress address, CancellationToken cancellationToken = default);
}
