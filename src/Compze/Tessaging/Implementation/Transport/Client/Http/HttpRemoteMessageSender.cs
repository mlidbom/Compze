using System;
using System.Threading.Tasks;
using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Abstractions.Serialization.Internal;
using Compze.Abstractions.Tessaging.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Transport.Internal;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Abstractions;
using Compze.Utilities.Threading.TasksCE;

namespace Compze.Tessaging.Implementation.Transport.Client.Http;

class HttpRemoteMessageSender(
   IRemoteApiTransportClient remoteApiTransportClient,
   HttpEndPointAddress remoteAddress,
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

   public async Task SendAsync(IExactlyOnceTommand tommand)
   {
      var outGoingMessage = TransportMessage.OutGoing.Create(tommand, _typeMapper, _serializer);
      _messagesInFlightTracker.SendingMessageOnTransport(outGoingMessage, _remoteEndpointId);
      await _transportClient.PostAsync(outGoingMessage, tommand, new Uri($"{_remoteAddress}{HttpConstants.Routes.Tessaging.Command}")).caf();
   }

   public async Task SendAsync(IExactlyOnceTevent tevent)
   {
      var message = TransportMessage.OutGoing.Create(tevent, _typeMapper, _serializer);
      _messagesInFlightTracker.SendingMessageOnTransport(message, _remoteEndpointId);
      await _transportClient.PostAsync(message, tevent, new Uri($"{_remoteAddress}{HttpConstants.Routes.Tessaging.Event}")).caf();
   }
}
