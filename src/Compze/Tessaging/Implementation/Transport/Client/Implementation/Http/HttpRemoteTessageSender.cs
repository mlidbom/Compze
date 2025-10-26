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
   IHttpTransportMessagePoster httpTransportMessagePoster,
   HttpEndPointAddress remoteAddress,
   ITypeMapper typeMapper,
   IRemotableTessageSerializer serializer,
   ITessagesInFlightTracker tessagesInFlightTracker,
   EndpointId remoteEndpointId) : IExactlyOnceTessageSender
{
   readonly IHttpTransportMessagePoster _transportMessagePoster = httpTransportMessagePoster;
   readonly ITypeMapper _typeMapper = typeMapper;
   readonly IRemotableTessageSerializer _serializer = serializer;
   readonly ITessagesInFlightTracker _tessagesInFlightTracker = tessagesInFlightTracker;
   readonly Uri _remoteAddress = remoteAddress.Uri;
   readonly EndpointId _remoteEndpointId = remoteEndpointId;

   public async Task SendAsync(IExactlyOnceTommand tommand)
   {
      var outGoingTessage = TransportTessage.OutGoing.Create(tommand, _typeMapper, _serializer);
      _tessagesInFlightTracker.SendingTessageOnTransport(outGoingTessage, _remoteEndpointId);
      await _transportMessagePoster.PostAsync(outGoingTessage, tommand, new Uri($"{_remoteAddress}{HttpConstants.Routes.Tessaging.Tommand}")).caf();
   }

   public async Task SendAsync(IExactlyOnceTevent tevent)
   {
      var tessage = TransportTessage.OutGoing.Create(tevent, _typeMapper, _serializer);
      _tessagesInFlightTracker.SendingTessageOnTransport(tessage, _remoteEndpointId);
      await _transportMessagePoster.PostAsync(tessage, tevent, new Uri($"{_remoteAddress}{HttpConstants.Routes.Tessaging.Tevent}")).caf();
   }
}
