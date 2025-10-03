using System;
using System.Net.Http;
using System.Threading.Tasks;
using Compze.Tessaging.Tessaging.Buses.Implementation;

namespace Compze.Tessaging.Tessaging.Buses.Http;

interface IHttpApiClient
{
   Task<TResult> PostAsync<TResult>(TransportMessage.OutGoing message, object realMessage, Uri requestUri);
   Task<HttpResponseMessage> PostAsync(TransportMessage.OutGoing message, object realMessage, Uri requestUri);
}
