using System;
using System.Threading.Tasks;
using Compze.Abstractions.Internal.Refactoring.Naming;
using Compze.Serialization;
using Compze.Tessaging.Abstractions;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Threading.TasksCE;

namespace Compze.Tessaging.Hosting.Implementation.Http;

class RpcClient(IHttpApiClient httpClient, EndPointAddress remoteAddress, ITypeMapper typeMapper, IRemotableMessageSerializer serializer, IMessagesInFlightTracker messagesInFlightTracker) : IRpcClient
{
   readonly IHttpApiClient _client = httpClient;
   readonly ITypeMapper _typeMapper = typeMapper;
   readonly IRemotableMessageSerializer _serializer = serializer;
   readonly IMessagesInFlightTracker _messagesInFlightTracker = messagesInFlightTracker;
   readonly string _remoteAddress = remoteAddress.AspNetAddress;

   public async Task<TResult> PostAsync<TResult>(IAtMostOnceCommand<TResult> command)
   {
      var message = TransportMessage.OutGoing.Create(command, _typeMapper, _serializer);
      _messagesInFlightTracker.SendingMessageOnTransport(message);
      return await _client.PostAsync<TResult>(message, command, new Uri($"{_remoteAddress}{HttpConstants.Routes.Rpc.CommandWithResult}")).caf();
   }

   public async Task PostAsync(IAtMostOnceHypermediaCommand command)
   {
      var outGoingMessage = TransportMessage.OutGoing.Create(command, _typeMapper, _serializer);
      _messagesInFlightTracker.SendingMessageOnTransport(outGoingMessage);
      await _client.PostAsync(outGoingMessage, command, new Uri($"{_remoteAddress}{HttpConstants.Routes.Rpc.CommandNoResult}")).caf();
   }

   public async Task<TResult> QueryAsync<TResult>(IRemotableQuery<TResult> query)
   {
      var message = TransportMessage.OutGoing.Create(query, _typeMapper, _serializer);
      _messagesInFlightTracker.SendingMessageOnTransport(message);
      return await _client.PostAsync<TResult>(message, query, new Uri($"{_remoteAddress}{HttpConstants.Routes.Rpc.Query}")).caf();
   }
}