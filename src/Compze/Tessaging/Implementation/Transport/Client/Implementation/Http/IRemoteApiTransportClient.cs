using System;
using System.Net.Http;
using System.Threading.Tasks;
using Compze.Tessaging.Implementation.Transport.Abstractions;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation.Http;

interface IHttpApiTransportClient
{
   Task<TResult> PostAsync<TResult>(TransportTessage.OutGoing tessage, object realTessage, Uri requestUri);
   Task<HttpResponseMessage> PostAsync(TransportTessage.OutGoing tessage, object realTessage, Uri requestUri);
}
