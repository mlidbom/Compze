using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Compze.Tessaging.Hosting.Implementation.Abstractions.Transport.Client;

interface IRemoteApiTransportClient
{
   Task<TResult> PostAsync<TResult>(TransportMessage.OutGoing message, object realMessage, Uri requestUri);
   Task<HttpResponseMessage> PostAsync(TransportMessage.OutGoing message, object realMessage, Uri requestUri);
}
