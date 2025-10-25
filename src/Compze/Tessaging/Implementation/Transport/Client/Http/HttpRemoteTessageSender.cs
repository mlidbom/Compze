using System;
using System.Threading.Tasks;
using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Abstractions.Serialization.Internal;
using Compze.Abstractions.Tessaging.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Transport.Internal;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Abstractions;
using Compze.Utilities.Threading.TasksCE;

namespace Compze.Tessaging.Implementation.Transport.Client.Http;

class HttpRemoteTessageSender(
   IRemoteApiTransportClient remoteApiTransportClient,
   HttpEndPointAddress remoteAddress,
   ITypeMapper typeMapper,
   IRemotableTessageSerializer serializer,
   ITessagesInFlightTracker tessagesInFlightTracker,
   EndpointId remoteEndpointId) : IRemoteTessageSender
{
   readonly IRemoteApiTransportClient _transportClient = remoteApiTransportClient;
   readonly ITypeMapper _typeMapper = typeMapper;
   readonly IRemotableTessageSerializer _serializer = serializer;
   readonly ITessagesInFlightTracker _tessagesInFlightTracker = tessagesInFlightTracker;
   readonly string _remoteAddress = remoteAddress.AspNetAddress;
   readonly EndpointId _remoteEndpointId = remoteEndpointId;

   public async Task SendAsync(IExactlyOnceTommand tommand)
   {
      var outGoingTessage = TransportTessage.OutGoing.Create(tommand, _typeMapper, _serializer);
      _tessagesInFlightTracker.SendingTessageOnTransport(outGoingTessage, _remoteEndpointId);
      await _transportClient.PostAsync(outGoingTessage, tommand, new Uri($"{_remoteAddress}{HttpConstants.Routes.Tessaging.Command}")).caf();
   }

   public async Task SendAsync(IExactlyOnceTevent tevent)
   {
      var tessage = TransportTessage.OutGoing.Create(tevent, _typeMapper, _serializer);
      _tessagesInFlightTracker.SendingTessageOnTransport(tessage, _remoteEndpointId);
      await _transportClient.PostAsync(tessage, tevent, new Uri($"{_remoteAddress}{HttpConstants.Routes.Tessaging.Event}")).caf();
   }
}
