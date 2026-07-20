using Compze.Tessaging.Endpoints.Discovery;

namespace Compze.Tessaging.Internal.Transport.Advertisement;

interface IEndpointInformationQueryTransport
{
   Task<TResult> GetAsync<TResult>(ITuery<TResult> query, EndpointAddress address, CancellationToken cancellationToken = default);
}
