using System;
using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;
using Composable.Messaging.Buses.Implementation.Http;
using Composable.Serialization;
using Composable.SystemCE.ThreadingCE.TasksCE;

namespace Composable.Messaging.Buses.Http;

class RpcClient(IHttpApiClient httpClient) : IRpcClient, IMessageSender
{
   readonly IHttpApiClient _client = httpClient;

   public async Task<TResult> PostAsync<TResult>(EndPointAddress address, TransportMessage.OutGoing message, IAtMostOnceCommand<TResult> query, IRemotableMessageSerializer serializer) =>
      await _client.PostAsync<TResult>(message, query, serializer, new Uri($"{address.AspNetAddress}{HttpConstants.Routes.Rpc.CommandWithResult}")).CaF();

   public async Task PostAsync(EndPointAddress address, TransportMessage.OutGoing message, IAtMostOnceHypermediaCommand query, IRemotableMessageSerializer serializer) =>
      await _client.PostAsync(message, query, new Uri($"{address.AspNetAddress}{HttpConstants.Routes.Rpc.CommandNoResult}")).CaF();

   public async Task<TResult> QueryAsync<TResult>(EndPointAddress address, TransportMessage.OutGoing message, IRemotableQuery<TResult> query, IRemotableMessageSerializer serializer) =>
      await _client.PostAsync<TResult>(message, query, serializer, new Uri($"{address.AspNetAddress}{HttpConstants.Routes.Rpc.Query}")).CaF();

   public async Task SendAsync(EndPointAddress address, TransportMessage.OutGoing message, IExactlyOnceCommand command, IRemotableMessageSerializer serializer) =>
      await _client.PostAsync(message, command, new Uri($"{address.AspNetAddress}{HttpConstants.Routes.Messaging.Command}")).CaF();

   public async Task SendAsync(EndPointAddress address, TransportMessage.OutGoing message, IExactlyOnceEvent @event, IRemotableMessageSerializer serializer) =>
      await _client.PostAsync(message, @event, new Uri($"{address.AspNetAddress}{HttpConstants.Routes.Messaging.Event}")).CaF();
}
