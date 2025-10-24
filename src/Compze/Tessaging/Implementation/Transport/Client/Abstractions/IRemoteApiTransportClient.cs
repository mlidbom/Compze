using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Compze.Tessaging.Implementation.Transport.Client.Abstractions;

interface IRemoteApiTransportClient
{
   Task<TResult> PostAsync<TResult>(TransportMessage.OutGoing message, object realMessage, Uri requestUri);
   Task<HttpResponseMessage> PostAsync(TransportMessage.OutGoing message, object realMessage, Uri requestUri);
}
