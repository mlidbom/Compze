using Compze.Tessaging.Endpoints.Discovery;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging._private.Transport.Advertisement;

interface IEndpointInformationQueryTransport
{
   Task<TResult> GetAsync<TResult>(ITuery<TResult> query, EndpointAddress address, CancellationToken cancellationToken = default);
}
