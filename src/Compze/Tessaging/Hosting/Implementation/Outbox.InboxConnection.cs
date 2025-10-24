using System.Threading.Tasks;
using Compze.Abstractions.Internal.Refactoring.Naming;
using Compze.Tessaging.Abstractions;
using Compze.Serialization;
using Compze.Tessaging.Hosting.Implementation.Abstractions;
using Compze.Tessaging.Hosting.Implementation.Http;
using Compze.Utilities.Threading.TasksCE;
using Compze.Tessaging.Hosting.Abstractions.Transport;
using Compze.Tessaging.Hosting.Implementation.Abstractions.Transport.Client;

namespace Compze.Tessaging.Hosting.Implementation;

partial class Outbox
{
   internal class InboxConnection(IMessagesInFlightTracker messagesInFlightTracker, EndPointAddress remoteAddress, ITypeMapper typeMapper, IRemotableMessageSerializer serializer, IRemoteApiTransportClient remoteApiTransportClient) : IInboxConnection
   {
      MessageTypesInternal.EndpointInformation? _endpointInformation = null;
      IRemoteApiClient? _rpcClient;
      IRemoteMessageSender? _messageSender;
      readonly IMessagesInFlightTracker _messagesInFlightTracker = messagesInFlightTracker;
      readonly EndPointAddress _remoteAddress = remoteAddress;
      readonly ITypeMapper _typeMapper = typeMapper;
      readonly IRemotableMessageSerializer _serializer = serializer;
      readonly IRemoteApiTransportClient _remoteApiTransportClient = remoteApiTransportClient;

      public MessageTypesInternal.EndpointInformation EndpointInformation => _endpointInformation!;

      public async Task SendAsync(IExactlyOnceEvent @event) => await _messageSender!.SendAsync(@event).caf();
      public async Task SendAsync(IExactlyOnceCommand command) => await _messageSender!.SendAsync(command).caf();

      public async Task<TCommandResult> PostAsync<TCommandResult>(IAtMostOnceCommand<TCommandResult> command) => await _rpcClient!.PostAsync(command).caf();
      public async Task PostAsync(IAtMostOnceHypermediaCommand command) => await _rpcClient!.PostAsync(command).caf();
      public async Task<TQueryResult> GetAsync<TQueryResult>(IRemotableQuery<TQueryResult> query) => await _rpcClient!.QueryAsync(query).caf();

      internal async Task InitAsync()
      {
         (_rpcClient, _endpointInformation) = await RemoteApiClient.BootstrapConnectionToEndpoint(_remoteApiTransportClient, _remoteAddress, _typeMapper, _serializer, _messagesInFlightTracker).caf();
         _messageSender = new RemoteMessageSender(_remoteApiTransportClient, _remoteAddress, _typeMapper, _serializer, _messagesInFlightTracker, _endpointInformation.Id);
      }

      public void Dispose() {}
   }
}
