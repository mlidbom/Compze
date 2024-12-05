using System.Threading.Tasks;
using Compze.Contracts.Deprecated;
using Compze.Functional;
using Compze.Messaging.Buses.Http;
using Compze.Messaging.Buses.Implementation.Http;
using Compze.Refactoring.Naming;
using Compze.Serialization;
using Compze.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Messaging.Buses.Implementation;

partial class Outbox
{
   internal class InboxConnection(IGlobalBusStateTracker globalBusStateTracker, EndPointAddress remoteAddress, ITypeMapper typeMapper, IRemotableMessageSerializer serializer, IHttpApiClient httpApiClient) : IInboxConnection
   {
      MessageTypes.Internal.EndpointInformation? _endpointInformation = null;
      readonly IRpcClient _rpcClient = new RpcClient(httpApiClient, remoteAddress, typeMapper, serializer, globalBusStateTracker);
      readonly IMessageSender _messageSender = new MessageSender(httpApiClient, remoteAddress, typeMapper, serializer, globalBusStateTracker);

      public MessageTypes.Internal.EndpointInformation EndpointInformation => Contract.Assert.That(_endpointInformation != null, $"{nameof(Init)} must be called before {nameof(EndpointInformation)} can be accessed")
                                                                                      .value(_endpointInformation!);

      public async Task SendAsync(IExactlyOnceEvent @event) => await _messageSender.SendAsync(@event).CaF();
      public async Task SendAsync(IExactlyOnceCommand command) => await _messageSender.SendAsync(command).CaF();

      public async Task<TCommandResult> PostAsync<TCommandResult>(IAtMostOnceCommand<TCommandResult> command) => await _rpcClient.PostAsync(command).CaF();
      public async Task PostAsync(IAtMostOnceHypermediaCommand command) => await _rpcClient.PostAsync(command).CaF();
      public async Task<TQueryResult> GetAsync<TQueryResult>(IRemotableQuery<TQueryResult> query) => await _rpcClient.QueryAsync(query).CaF();

      internal async Task Init() => _endpointInformation = await GetAsync(new MessageTypes.Internal.EndpointInformationQuery()).CaF();

      public void Dispose() {}
   }
}
