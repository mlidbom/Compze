using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Composable.Messaging.Buses.Http;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.SystemCE.ThreadingCE.ResourceAccess;
using Composable.SystemCE.ThreadingCE.TasksCE;

namespace Composable.Messaging.Buses.Implementation;

partial class Outbox
{
   internal class InboxConnection : IInboxConnection
   {
      readonly IThreadShared<InboxConnectionState> _state;
      public MessageTypes.Internal.EndpointInformation EndpointInformation { get; private set; }
      readonly ITypeMapper _typeMapper;
      readonly IRemotableMessageSerializer _serializer;
      readonly IGlobalBusStateTracker _globalBusStateTracker;
      readonly EndPointAddress _remoteAddress;
      readonly IComposableHttpClientFactoryProvider _httpClient;

      public async Task SendAsync(IExactlyOnceEvent @event)
      {
         var outGoingMessage = TransportMessage.OutGoing.Create(@event, _typeMapper, _serializer);
         _globalBusStateTracker.SendingMessageOnTransport(outGoingMessage);
         await _httpClient.PostAsync(_remoteAddress, outGoingMessage, @event, _serializer).CaF();
      }

      public async Task SendAsync(IExactlyOnceCommand command)
      {
         var outGoingMessage = TransportMessage.OutGoing.Create(command, _typeMapper, _serializer);
         _globalBusStateTracker.SendingMessageOnTransport(outGoingMessage);
         await _httpClient.PostAsync(_remoteAddress, outGoingMessage, command, _serializer).CaF();
      }

      public async Task<TCommandResult> PostAsync<TCommandResult>(IAtMostOnceCommand<TCommandResult> command)
      {
         var outGoingMessage = TransportMessage.OutGoing.Create(command, _typeMapper, _serializer);
         _globalBusStateTracker.SendingMessageOnTransport(outGoingMessage);
         return await _httpClient.PostAsync(_remoteAddress, outGoingMessage, command, _serializer).CaF();
      }

      public async Task PostAsync(IAtMostOnceHypermediaCommand command)
      {
         var outGoingMessage = TransportMessage.OutGoing.Create(command, _typeMapper, _serializer);
         _globalBusStateTracker.SendingMessageOnTransport(outGoingMessage);
         await _httpClient.PostAsync(_remoteAddress, outGoingMessage, command, _serializer).CaF();
      }

      public async Task<TQueryResult> GetAsync<TQueryResult>(IRemotableQuery<TQueryResult> query)
      {
         var outGoingMessage = TransportMessage.OutGoing.Create(query, _typeMapper, _serializer);
         _globalBusStateTracker.SendingMessageOnTransport(outGoingMessage);
         return await _httpClient.QueryAsync(_remoteAddress, outGoingMessage, query, _serializer).CaF();
      }

      internal async Task Init() => EndpointInformation = await GetAsync(new MessageTypes.Internal.EndpointInformationQuery()).CaF();

#pragma warning disable 8618 //Refactor: This really should not be suppressed. We do have a bad design that might cause null reference exceptions here if Init has not been called.
      internal InboxConnection(IGlobalBusStateTracker globalBusStateTracker,
#pragma warning restore 8618
                               EndPointAddress remoteAddress,
                               IComposableHttpClientFactoryProvider httpClient,
                               ITypeMapper typeMapper,
                               IRemotableMessageSerializer serializer)
      {
         _serializer = serializer;
         _typeMapper = typeMapper;
         _globalBusStateTracker = globalBusStateTracker;
         _remoteAddress = remoteAddress;
         _httpClient = httpClient;
         _state = ThreadShared.WithDefaultTimeout(new InboxConnectionState());
      }

      public void Dispose()
      {
      }

      class InboxConnectionState
      {
         internal readonly Dictionary<Guid, AsyncTaskCompletionSource<Func<object>>> ExpectedResponseTasks = new();
         internal readonly Dictionary<Guid, AsyncTaskCompletionSource> ExpectedCompletionTasks = new();
      }
   }
}
