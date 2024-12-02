using System;
using System.Threading.Tasks;
using Compze.Messaging.Buses.Http;
using Compze.Refactoring.Naming;
using Compze.Serialization;
using Compze.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Messaging.Buses.Implementation.Http;

class RpcClient(IHttpApiClient httpClient, EndPointAddress remoteAddress, ITypeMapper typeMapper, IRemotableMessageSerializer serializer, IGlobalBusStateTracker globalBusStateTracker) : IRpcClient
{
   readonly IHttpApiClient _client = httpClient;
   readonly ITypeMapper _typeMapper = typeMapper;
   readonly IRemotableMessageSerializer _serializer = serializer;
   readonly IGlobalBusStateTracker _globalBusStateTracker = globalBusStateTracker;
   readonly string _remoteAddress = remoteAddress.AspNetAddress;

   public async Task<TResult> PostAsync<TResult>(IAtMostOnceCommand<TResult> command)
   {
      var message = TransportMessage.OutGoing.Create(command, _typeMapper, _serializer);
      _globalBusStateTracker.SendingMessageOnTransport(message);
      return await _client.PostAsync<TResult>(message, command, new Uri($"{_remoteAddress}{HttpConstants.Routes.Rpc.CommandWithResult}")).CaF();
   }

   public async Task PostAsync(IAtMostOnceHypermediaCommand command)
   {
      var outGoingMessage = TransportMessage.OutGoing.Create(command, _typeMapper, _serializer);
      _globalBusStateTracker.SendingMessageOnTransport(outGoingMessage);
      await _client.PostAsync(outGoingMessage, command, new Uri($"{_remoteAddress}{HttpConstants.Routes.Rpc.CommandNoResult}")).CaF();
   }

   public async Task<TResult> QueryAsync<TResult>(IRemotableQuery<TResult> query)
   {
      var message = TransportMessage.OutGoing.Create(query, _typeMapper, _serializer);
      _globalBusStateTracker.SendingMessageOnTransport(message);
      return await _client.PostAsync<TResult>(message, query, new Uri($"{_remoteAddress}{HttpConstants.Routes.Rpc.Query}")).CaF();
   }
}