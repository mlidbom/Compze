using Compze.Abstractions.Hosting.Public;

namespace Compze.ServiceBus.Implementation.Transport.Abstractions;

interface ITransportMessagePoster
{
   Task PostAsync(TransportTessage.OutGoing tessage, EndpointAddress endPointAddress);
}
