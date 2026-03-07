using Compze.Core.Tessaging.Transport.Internal;

namespace Compze.Tessaging.Implementation.Transport.Abstractions;

interface ITransportMessagePoster
{
   Task PostAsync(TransportTessage.OutGoing tessage, EndPointAddress endPointAddress);
}
