using System.Threading.Tasks;
using Compze.Abstractions.Internal.Refactoring.Naming;
using Compze.Tessaging.Abstractions;
using Compze.Serialization;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.Implementation.Abstractions;
using Compze.Tessaging.Hosting.Implementation.Http;
using Compze.Utilities.Threading.TasksCE;

namespace Compze.Tessaging.Hosting.Implementation;

partial class Outbox
{
   internal class InboxConnection(IMessagesInFlightTracker messagesInFlightTracker, EndPointAddress remoteAddress, ITypeMapper typeMapper, IRemotableMessageSerializer serializer, IHttpApiClient httpApiClient) : IInboxConnection
   {
      MessageTypesInternal.EndpointInformation? _endpointInformation = null;
      IRpcClient? _rpcClient;
      IMessageSender? _messageSender;
      readonly IMessagesInFlightTracker _messagesInFlightTracker = messagesInFlightTracker;
      readonly EndPointAddress _remoteAddress = remoteAddress;
      readonly ITypeMapper _typeMapper = typeMapper;
      readonly IRemotableMessageSerializer _serializer = serializer;
      readonly IHttpApiClient _httpApiClient = httpApiClient;

      public MessageTypesInternal.EndpointInformation EndpointInformation => _endpointInformation!;

      public async Task SendAsync(IExactlyOnceEvent @event) => await _messageSender!.SendAsync(@event).caf();
      public async Task SendAsync(IExactlyOnceCommand command) => await _messageSender!.SendAsync(command).caf();

      public async Task<TCommandResult> PostAsync<TCommandResult>(IAtMostOnceCommand<TCommandResult> command) => await _rpcClient!.PostAsync(command).caf();
      public async Task PostAsync(IAtMostOnceHypermediaCommand command) => await _rpcClient!.PostAsync(command).caf();
      public async Task<TQueryResult> GetAsync<TQueryResult>(IRemotableQuery<TQueryResult> query) => await _rpcClient!.QueryAsync(query).caf();

      internal async Task InitAsync()
      {
         (_rpcClient, _endpointInformation) = await RpcClient.BootstrapConnectionToEndpoint(_httpApiClient, _remoteAddress, _typeMapper, _serializer, _messagesInFlightTracker).caf();
         _messageSender = new MessageSender(_httpApiClient, _remoteAddress, _typeMapper, _serializer, _messagesInFlightTracker, _endpointInformation.Id);
      }

      public void Dispose() {}
   }
}
