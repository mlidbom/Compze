using System.Threading.Tasks;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Serialization.Internal;
using Compze.Core.Tessaging.Public;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Http;
using Compze.Utilities.Threading.TasksCE;

namespace Compze.Tessaging.Implementation.Outbox;

partial class Outbox
{
   internal class InboxConnection(ITessagesInFlightTracker tessagesInFlightTracker, HttpEndPointAddress remoteAddress, ITypeMapper typeMapper, IRemotableTessageSerializer serializer, IRemoteApiTransportClient remoteApiTransportClient) : IInboxConnection
   {
      TessageTypesInternal.EndpointInformation? _endpointInformation = null;
      IRemoteApiClient? _rpcClient;
      IRemoteTessageSender? _tessageSender;
      readonly ITessagesInFlightTracker _tessagesInFlightTracker = tessagesInFlightTracker;
      readonly HttpEndPointAddress _remoteAddress = remoteAddress;
      readonly ITypeMapper _typeMapper = typeMapper;
      readonly IRemotableTessageSerializer _serializer = serializer;
      readonly IRemoteApiTransportClient _remoteApiTransportClient = remoteApiTransportClient;

      public TessageTypesInternal.EndpointInformation EndpointInformation => _endpointInformation!;

      public async Task SendAsync(IExactlyOnceTevent tevent) => await _tessageSender!.SendAsync(tevent).caf();
      public async Task SendAsync(IExactlyOnceTommand tommand) => await _tessageSender!.SendAsync(tommand).caf();

      public async Task<TTommandResult> PostAsync<TTommandResult>(IAtMostOnceTommand<TTommandResult> tommand) => await _rpcClient!.PostAsync(tommand).caf();
      public async Task PostAsync(IAtMostOnceHypermediaTommand tommand) => await _rpcClient!.PostAsync(tommand).caf();
      public async Task<TTueryResult> GetAsync<TTueryResult>(IRemotableTuery<TTueryResult> tuery) => await _rpcClient!.TueryAsync(tuery).caf();

      internal async Task InitAsync()
      {
         (_rpcClient, _endpointInformation) = await HttpApiClient.BootstrapConnectionToEndpoint(_remoteApiTransportClient, _remoteAddress, _typeMapper, _serializer, _tessagesInFlightTracker).caf();
         _tessageSender = new HttpRemoteTessageSender(_remoteApiTransportClient, _remoteAddress, _typeMapper, _serializer, _tessagesInFlightTracker, _endpointInformation.Id);
      }

      public void Dispose() {}
   }
}
