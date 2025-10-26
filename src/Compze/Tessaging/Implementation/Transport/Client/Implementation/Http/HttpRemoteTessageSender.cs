using System;
using System.Threading.Tasks;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Serialization.Internal;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Public;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Internal;
using Compze.Utilities.Threading.TasksCE;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation.Http;

class HttpExactlyOnceTessageSender(
   IHttpApiTransportClient httpApiTransportClient,
   HttpEndPointAddress remoteAddress,
   ITypeMapper typeMapper,
   IRemotableTessageSerializer serializer,
   ITessagesInFlightTracker tessagesInFlightTracker,
   EndpointId remoteEndpointId) : IExactlyOnceTessageSender
{
   readonly IHttpApiTransportClient _transportClient = httpApiTransportClient;
   readonly ITypeMapper _typeMapper = typeMapper;
   readonly IRemotableTessageSerializer _serializer = serializer;
   readonly ITessagesInFlightTracker _tessagesInFlightTracker = tessagesInFlightTracker;
   readonly string _remoteAddress = remoteAddress.AspNetAddress;
   readonly EndpointId _remoteEndpointId = remoteEndpointId;

   public async Task SendAsync(IExactlyOnceTommand tommand)
   {
      var outGoingTessage = TransportTessage.OutGoing.Create(tommand, _typeMapper, _serializer);
      _tessagesInFlightTracker.SendingTessageOnTransport(outGoingTessage, _remoteEndpointId);
      await _transportClient.PostAsync(outGoingTessage, tommand, new Uri($"{_remoteAddress}{HttpConstants.Routes.Tessaging.Tommand}")).caf();
   }

   public async Task SendAsync(IExactlyOnceTevent tevent)
   {
      var tessage = TransportTessage.OutGoing.Create(tevent, _typeMapper, _serializer);
      _tessagesInFlightTracker.SendingTessageOnTransport(tessage, _remoteEndpointId);
      await _transportClient.PostAsync(tessage, tevent, new Uri($"{_remoteAddress}{HttpConstants.Routes.Tessaging.Tevent}")).caf();
   }
}
