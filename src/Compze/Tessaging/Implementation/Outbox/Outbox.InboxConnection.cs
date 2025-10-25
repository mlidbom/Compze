using System.Threading.Tasks;
using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Abstractions.Serialization.Internal;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Transport.Internal;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Http;
using Compze.Utilities.Threading.TasksCE;

namespace Compze.Tessaging.Implementation.Outbox;

partial class Outbox
{
   internal class InboxConnection(IMessagesInFlightTracker messagesInFlightTracker, HttpEndPointAddress remoteAddress, ITypeMapper typeMapper, IRemotableMessageSerializer serializer, IRemoteApiTransportClient remoteApiTransportClient) : IInboxConnection
   {
      MessageTypesInternal.EndpointInformation? _endpointInformation = null;
      IRemoteApiClient? _rpcClient;
      IRemoteMessageSender? _messageSender;
      readonly IMessagesInFlightTracker _messagesInFlightTracker = messagesInFlightTracker;
      readonly HttpEndPointAddress _remoteAddress = remoteAddress;
      readonly ITypeMapper _typeMapper = typeMapper;
      readonly IRemotableMessageSerializer _serializer = serializer;
      readonly IRemoteApiTransportClient _remoteApiTransportClient = remoteApiTransportClient;

      public MessageTypesInternal.EndpointInformation EndpointInformation => _endpointInformation!;

      public async Task SendAsync(IExactlyOnceTevent tevent) => await _messageSender!.SendAsync(tevent).caf();
      public async Task SendAsync(IExactlyOnceTommand tommand) => await _messageSender!.SendAsync(tommand).caf();

      public async Task<TCommandResult> PostAsync<TCommandResult>(IAtMostOnceTommand<TCommandResult> tommand) => await _rpcClient!.PostAsync(tommand).caf();
      public async Task PostAsync(IAtMostOnceHypermediaTommand tommand) => await _rpcClient!.PostAsync(tommand).caf();
      public async Task<TQueryResult> GetAsync<TQueryResult>(IRemotableTuery<TQueryResult> tuery) => await _rpcClient!.QueryAsync(tuery).caf();

      internal async Task InitAsync()
      {
         (_rpcClient, _endpointInformation) = await HttpApiClient.BootstrapConnectionToEndpoint(_remoteApiTransportClient, _remoteAddress, _typeMapper, _serializer, _messagesInFlightTracker).caf();
         _messageSender = new HttpRemoteMessageSender(_remoteApiTransportClient, _remoteAddress, _typeMapper, _serializer, _messagesInFlightTracker, _endpointInformation.Id);
      }

      public void Dispose() {}
   }
}
