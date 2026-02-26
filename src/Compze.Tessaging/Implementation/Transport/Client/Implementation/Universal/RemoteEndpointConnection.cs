using System;
using System.Threading.Tasks;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Serialization.Internal;
using Compze.Core.Tessaging.Public;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Implementation.Http;
using Compze.Tessaging.Implementation.Transport.Client.Internal;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation.Universal;

public class RemoteEndpointConnection : ITessagingInboxConnection, IDisposable
{
   public TessageTypesInternal.EndpointInformation EndpointInformation { get; private set; } = null!;
   public IRemoteApiEndpointClient ApiClient { get; private set; } = null!;
   IExactlyOnceTessageSender ExactlyOnceSender { get; set; } = null!;

   readonly ITessagesInFlightTracker _tessagesInFlightTracker;
   readonly EndPointAddress _remoteAddress;
   readonly ITypeMapper _typeMapper;
   readonly IRemotableTessageSerializer _serializer;
   readonly ITransportMessagePoster _transportMessagePoster;

   public RemoteEndpointConnection(
      ITessagesInFlightTracker tessagesInFlightTracker,
      EndPointAddress remoteAddress,
      ITypeMapper typeMapper,
      IRemotableTessageSerializer serializer,
      ITransportMessagePoster transportMessagePoster)
   {
      _tessagesInFlightTracker = tessagesInFlightTracker;
      _remoteAddress = remoteAddress;
      _typeMapper = typeMapper;
      _serializer = serializer;
      _transportMessagePoster = transportMessagePoster;
   }

   public async Task InitAsync()
   {
      (var apiClient, var endpointInformation) = await ApiEndpointClient.BootstrapConnectionToEndpoint(_transportMessagePoster, _remoteAddress, _typeMapper, _serializer).caf();
      ApiClient = apiClient;
      EndpointInformation = endpointInformation;
      ExactlyOnceSender = new HttpExactlyOnceTessageSender(_transportMessagePoster, _remoteAddress, _typeMapper, _serializer, _tessagesInFlightTracker, endpointInformation.Id);
   }

   // ITessagingInboxConnection
   public async Task SendAsync(IExactlyOnceTevent tevent) => await ExactlyOnceSender.SendAsync(tevent).caf();
   public async Task SendAsync(IExactlyOnceTommand tommand) => await ExactlyOnceSender.SendAsync(tommand).caf();

   public void Dispose() {}
}
