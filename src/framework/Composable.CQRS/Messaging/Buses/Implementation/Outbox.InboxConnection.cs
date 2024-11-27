using System.Threading.Tasks;
using Composable.Contracts;
using Composable.Functional;
using Composable.Messaging.Buses.Http;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.SystemCE.ThreadingCE.TasksCE;

namespace Composable.Messaging.Buses.Implementation;

partial class Outbox
{
#pragma warning disable 8618 //Refactor: This really should not be suppressed. We do have a bad design that might cause null reference exceptions here if Init has not been called.
   internal class InboxConnection(IGlobalBusStateTracker globalBusStateTracker, EndPointAddress remoteAddress, ITypeMapper typeMapper, IRemotableMessageSerializer serializer, IRpcClient rpcClient, IMessageSender messageSender) : IInboxConnection
#pragma warning restore 8618
   {
      MessageTypes.Internal.EndpointInformation? _endpointInformation = null;

      public MessageTypes.Internal.EndpointInformation EndpointInformation => Contract.Assert.That(_endpointInformation != null, $"{nameof(Init)} must be called before {nameof(EndpointInformation)} can be accessed")
                                                                                      .then(_endpointInformation!);

      readonly ITypeMapper _typeMapper = typeMapper;
      readonly IRemotableMessageSerializer _serializer = serializer;
      readonly IRpcClient _rpcClient = rpcClient;
      readonly IMessageSender _messageSender = messageSender;
      readonly IGlobalBusStateTracker _globalBusStateTracker = globalBusStateTracker;
      readonly EndPointAddress _remoteAddress = remoteAddress;

      public async Task SendAsync(IExactlyOnceEvent @event)
      {
         var outGoingMessage = TransportMessage.OutGoing.Create(@event, _typeMapper, _serializer);
         _globalBusStateTracker.SendingMessageOnTransport(outGoingMessage);
         await _messageSender.SendAsync(_remoteAddress, outGoingMessage, @event, _serializer).CaF();
      }

      public async Task SendAsync(IExactlyOnceCommand command)
      {
         var outGoingMessage = TransportMessage.OutGoing.Create(command, _typeMapper, _serializer);
         _globalBusStateTracker.SendingMessageOnTransport(outGoingMessage);
         await _messageSender.SendAsync(_remoteAddress, outGoingMessage, command, _serializer).CaF();
      }

      public async Task<TCommandResult> PostAsync<TCommandResult>(IAtMostOnceCommand<TCommandResult> command)
      {
         var outGoingMessage = TransportMessage.OutGoing.Create(command, _typeMapper, _serializer);
         _globalBusStateTracker.SendingMessageOnTransport(outGoingMessage);
         return await _rpcClient.PostAsync(_remoteAddress, outGoingMessage, command, _serializer).CaF();
      }

      public async Task PostAsync(IAtMostOnceHypermediaCommand command)
      {
         var outGoingMessage = TransportMessage.OutGoing.Create(command, _typeMapper, _serializer);
         _globalBusStateTracker.SendingMessageOnTransport(outGoingMessage);
         await _rpcClient.PostAsync(_remoteAddress, outGoingMessage, command, _serializer).CaF();
      }

      public async Task<TQueryResult> GetAsync<TQueryResult>(IRemotableQuery<TQueryResult> query)
      {
         var outGoingMessage = TransportMessage.OutGoing.Create(query, _typeMapper, _serializer);
         _globalBusStateTracker.SendingMessageOnTransport(outGoingMessage);
         return await _rpcClient.QueryAsync(_remoteAddress, outGoingMessage, query, _serializer).CaF();
      }

      internal async Task Init() => _endpointInformation = await GetAsync(new MessageTypes.Internal.EndpointInformationQuery()).CaF();

      public void Dispose() {}
   }
}
