using Compze.Tessaging.Endpoints.Discovery;

namespace Compze.Tessaging.Internal.Transport;

interface ITransportMessagePoster
{
   Task PostAsync(TransportTessage.OutGoing tessage, EndpointAddress endPointAddress, CancellationToken cancellationToken = default);
}
