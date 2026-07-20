using Compze.Tessaging.Endpoints.Discovery;

namespace Compze.Tessaging.Internals.Transport.Abstractions;

interface ITransportMessagePoster
{
   Task PostAsync(TransportTessage.OutGoing tessage, EndpointAddress endPointAddress, CancellationToken cancellationToken = default);
}
