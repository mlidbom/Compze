using Compze.Core.Tessaging.Transport.Internal;

namespace Compze.Tessaging.Implementation.Transport.Abstractions;

interface ITransportMessagePoster
{
   Task<TResult> PostAsync<TResult>(TransportTessage.OutGoing tessage, EndPointAddress endPointAddress);
   Task PostAsync(TransportTessage.OutGoing tessage, EndPointAddress endPointAddress);
}
