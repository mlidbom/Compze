using System.Threading.Tasks;
using Compze.Abstractions.Internal.Refactoring.Naming;
using Compze.Tessaging.Abstractions;
using Compze.Serialization;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.Implementation.Abstractions;
using Compze.Tessaging.Hosting.Implementation.Http;
using Compze.Utilities.Contracts;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Tessaging.Hosting.Implementation;

partial class Outbox
{
   internal class InboxConnection(IMessagesInFlightTracker messagesInFlightTracker, EndPointAddress remoteAddress, ITypeMapper typeMapper, IRemotableMessageSerializer serializer, IHttpApiClient httpApiClient) : IInboxConnection
   {
      MessageTypesInternal.EndpointInformation? _endpointInformation = null;
      readonly IRpcClient _rpcClient = new RpcClient(httpApiClient, remoteAddress, typeMapper, serializer, messagesInFlightTracker);
      readonly IMessageSender _messageSender = new MessageSender(httpApiClient, remoteAddress, typeMapper, serializer, messagesInFlightTracker);

      public MessageTypesInternal.EndpointInformation EndpointInformation => Assert.State.Is(_endpointInformation != null, () => $"{nameof(Init)} must be called before {nameof(EndpointInformation)} can be accessed")
                                                                                      .then(_endpointInformation!);

      public async Task SendAsync(IExactlyOnceEvent @event) => await _messageSender.SendAsync(@event).caf();
      public async Task SendAsync(IExactlyOnceCommand command) => await _messageSender.SendAsync(command).caf();

      public async Task<TCommandResult> PostAsync<TCommandResult>(IAtMostOnceCommand<TCommandResult> command) => await _rpcClient.PostAsync(command).caf();
      public async Task PostAsync(IAtMostOnceHypermediaCommand command) => await _rpcClient.PostAsync(command).caf();
      public async Task<TQueryResult> GetAsync<TQueryResult>(IRemotableQuery<TQueryResult> query) => await _rpcClient.QueryAsync(query).caf();

      internal async Task Init() => _endpointInformation = await GetAsync(new MessageTypesInternal.EndpointInformationQuery()).caf();

      public void Dispose() {}
   }
}
