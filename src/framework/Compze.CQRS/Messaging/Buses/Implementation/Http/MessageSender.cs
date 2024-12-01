using System;
using System.Threading.Tasks;
using Composable.Messaging.Buses.Http;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.SystemCE.ThreadingCE.TasksCE;

namespace Composable.Messaging.Buses.Implementation.Http;

class MessageSender(IHttpApiClient httpClient, EndPointAddress remoteAddress, ITypeMapper typeMapper, IRemotableMessageSerializer serializer, IGlobalBusStateTracker globalBusStateTracker) : IMessageSender
{
   readonly IHttpApiClient _client = httpClient;
   readonly ITypeMapper _typeMapper = typeMapper;
   readonly IRemotableMessageSerializer _serializer = serializer;
   readonly IGlobalBusStateTracker _globalBusStateTracker = globalBusStateTracker;
   readonly string _remoteAddress = remoteAddress.AspNetAddress;

   public async Task SendAsync(IExactlyOnceCommand command)
   {
      var outGoingMessage = TransportMessage.OutGoing.Create(command, _typeMapper, _serializer);
      _globalBusStateTracker.SendingMessageOnTransport(outGoingMessage);
      await _client.PostAsync(outGoingMessage, command, new Uri($"{_remoteAddress}{HttpConstants.Routes.Messaging.Command}")).CaF();
   }

   public async Task SendAsync(IExactlyOnceEvent @event)
   {
      var message = TransportMessage.OutGoing.Create(@event, _typeMapper, _serializer);
      _globalBusStateTracker.SendingMessageOnTransport(message);
      await _client.PostAsync(message, @event, new Uri($"{_remoteAddress}{HttpConstants.Routes.Messaging.Event}")).CaF();
   }
}
