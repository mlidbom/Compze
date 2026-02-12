using System.Threading.Tasks;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Serialization.Internal;
using Compze.Core.Tessaging.Public;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Implementation.Http;
using Compze.Tessaging.Implementation.Transport.Client.Implementation.Universal;
using Compze.Tessaging.Implementation.Transport.Client.Internal;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Tessaging.Implementation.Outbox;

partial class Outbox
{
   internal class InboxConnection(
      ITessagesInFlightTracker tessagesInFlightTracker,
      EndPointAddress remoteAddress,
      ITypeMapper typeMapper,
      IRemotableTessageSerializer serializer,
      ITransportMessagePoster transportMessagePoster) : IInboxConnection
   {
      TessageTypesInternal.EndpointInformation? _endpointInformation = null;
      IRemoteApiEndpointClient? _remoteApiClient;
      IExactlyOnceTessageSender? _tessageSender;
      readonly ITessagesInFlightTracker _tessagesInFlightTracker = tessagesInFlightTracker;
      readonly EndPointAddress _remoteAddress = remoteAddress;
      readonly ITypeMapper _typeMapper = typeMapper;
      readonly IRemotableTessageSerializer _serializer = serializer;
      readonly ITransportMessagePoster _transportMessagePoster = transportMessagePoster;

      public TessageTypesInternal.EndpointInformation EndpointInformation => _endpointInformation!;

      public async Task SendAsync(IExactlyOnceTevent tevent) => await _tessageSender!.SendAsync(tevent).caf();
      public async Task SendAsync(IExactlyOnceTommand tommand) => await _tessageSender!.SendAsync(tommand).caf();

      public async Task<TTommandResult> PostAsync<TTommandResult>(IAtMostOnceTommand<TTommandResult> typermediaTommand) => await _remoteApiClient!.PostAsync(typermediaTommand).caf();
      public async Task PostAsync(IAtMostOnceTypermediaTommand tommand) => await _remoteApiClient!.PostAsync(tommand).caf();
      public async Task<TTueryResult> GetAsync<TTueryResult>(IRemotableTuery<TTueryResult> tuery) => await _remoteApiClient!.GetAsync(tuery).caf();

      internal async Task InitAsync()
      {
         (_remoteApiClient, _endpointInformation) = await ApiEndpointClient.BootstrapConnectionToEndpoint(_transportMessagePoster, _remoteAddress, _typeMapper, _serializer, _tessagesInFlightTracker).caf();
         _tessageSender = new HttpExactlyOnceTessageSender(_transportMessagePoster, _remoteAddress, _typeMapper, _serializer, _tessagesInFlightTracker, _endpointInformation.Id);
      }

      public void Dispose() {}
   }
}
