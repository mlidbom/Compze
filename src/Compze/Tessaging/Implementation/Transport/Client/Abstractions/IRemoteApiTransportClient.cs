using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Compze.Tessaging.Implementation.Transport.Client.Abstractions;

interface IRemoteApiTransportClient
{
   Task<TResult> PostAsync<TResult>(TransportTessage.OutGoing tessage, object realTessage, Uri requestUri);
   Task<HttpResponseMessage> PostAsync(TransportTessage.OutGoing tessage, object realTessage, Uri requestUri);
}
