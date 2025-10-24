using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Compze.Tessaging.Hosting.Implementation.Http;

interface IRemoteApiClient
{
   Task<TResult> PostAsync<TResult>(TransportMessage.OutGoing message, object realMessage, Uri requestUri);
   Task<HttpResponseMessage> PostAsync(TransportMessage.OutGoing message, object realMessage, Uri requestUri);
}
