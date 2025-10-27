using System.Threading.Tasks;
using Compze.Core.Tessaging.Transport.Internal;

namespace Compze.Tessaging.Implementation.Transport.Abstractions;

interface ITransportMessagePoster
{
   Task<TResult> PostAsync<TResult>(TransportTessage.OutGoing tessage, object realTessage, EndPointAddress endPointAddress);
   Task PostAsync(TransportTessage.OutGoing tessage, object realTessage, EndPointAddress endPointAddress);
}
