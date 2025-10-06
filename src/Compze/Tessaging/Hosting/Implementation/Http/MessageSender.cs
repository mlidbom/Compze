using System;
using System.Threading.Tasks;
using Compze.Abstractions.Internal.Refactoring.Naming;
using Compze.Tessaging.Abstractions;
using Compze.Hosting.Abstractions;
using Compze.Serialization;
using Compze.Tessaging.Hosting.Http;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Tessaging.Hosting.Implementation.Http;

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
      await _client.PostAsync(outGoingMessage, command, new Uri($"{_remoteAddress}{HttpConstants.Routes.Tessaging.Command}")).caf();
   }

   public async Task SendAsync(IExactlyOnceEvent @event)
   {
      var message = TransportMessage.OutGoing.Create(@event, _typeMapper, _serializer);
      _globalBusStateTracker.SendingMessageOnTransport(message);
      await _client.PostAsync(message, @event, new Uri($"{_remoteAddress}{HttpConstants.Routes.Tessaging.Event}")).caf();
   }
}
