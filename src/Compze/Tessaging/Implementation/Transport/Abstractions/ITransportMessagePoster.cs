using System;
using System.Threading.Tasks;

namespace Compze.Tessaging.Implementation.Transport.Abstractions;

interface ITransportMessagePoster
{
   Task<TResult> PostAsync<TResult>(TransportTessage.OutGoing tessage, object realTessage, Uri requestUri);
   Task PostAsync(TransportTessage.OutGoing tessage, object realTessage, Uri requestUri);
}
