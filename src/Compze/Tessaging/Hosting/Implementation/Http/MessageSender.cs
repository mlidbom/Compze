using System;
using System.Threading.Tasks;
using Compze.Abstractions.Internal.Refactoring.Naming;
using Compze.Serialization;
using Compze.Tessaging.Abstractions;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.Implementation.Abstractions;
using Compze.Utilities.Threading.TasksCE;

namespace Compze.Tessaging.Hosting.Implementation.Http;

class RemoteMessageSender(
   IRemoteApiTransportClient remoteApiTransportClient,
   EndPointAddress remoteAddress,
   ITypeMapper typeMapper,
   IRemotableMessageSerializer serializer,
   IMessagesInFlightTracker messagesInFlightTracker,
   EndpointId remoteEndpointId) : IRemoteMessageSender
{
   readonly IRemoteApiTransportClient _transportClient = remoteApiTransportClient;
   readonly ITypeMapper _typeMapper = typeMapper;
   readonly IRemotableMessageSerializer _serializer = serializer;
   readonly IMessagesInFlightTracker _messagesInFlightTracker = messagesInFlightTracker;
   readonly string _remoteAddress = remoteAddress.AspNetAddress;
   readonly EndpointId _remoteEndpointId = remoteEndpointId;

   public async Task SendAsync(IExactlyOnceCommand command)
   {
      var outGoingMessage = TransportMessage.OutGoing.Create(command, _typeMapper, _serializer);
      _messagesInFlightTracker.SendingMessageOnTransport(outGoingMessage, _remoteEndpointId);
      await _transportClient.PostAsync(outGoingMessage, command, new Uri($"{_remoteAddress}{HttpConstants.Routes.Tessaging.Command}")).caf();
   }

   public async Task SendAsync(IExactlyOnceEvent @event)
   {
      var message = TransportMessage.OutGoing.Create(@event, _typeMapper, _serializer);
      _messagesInFlightTracker.SendingMessageOnTransport(message, _remoteEndpointId);
      await _transportClient.PostAsync(message, @event, new Uri($"{_remoteAddress}{HttpConstants.Routes.Tessaging.Event}")).caf();
   }
}
