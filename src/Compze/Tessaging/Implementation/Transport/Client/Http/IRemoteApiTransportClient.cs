using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Compze.Tessaging.Implementation.Transport.Client.Http;

interface IHttpApiTransportClient
{
   Task<TResult> PostAsync<TResult>(TransportTessage.OutGoing tessage, object realTessage, Uri requestUri);
   Task<HttpResponseMessage> PostAsync(TransportTessage.OutGoing tessage, object realTessage, Uri requestUri);
}
