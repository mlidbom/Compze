using System;
using System.Threading.Tasks;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Serialization.Internal;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Public;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Abstractions;
using Compze.Utilities.Threading.TasksCE;

namespace Compze.Tessaging.Implementation.Transport.Client.Http;

class HttpApiClient(
   IRemoteApiTransportClient remoteApiTransportClient,
   HttpEndPointAddress remoteAddress,
   ITypeMapper typeMapper,
   IRemotableTessageSerializer serializer,
   ITessagesInFlightTracker tessagesInFlightTracker,
   EndpointId remoteEndpointId) : IRemoteApiClient
{
   readonly IRemoteApiTransportClient _transportClient = remoteApiTransportClient;
   readonly ITypeMapper _typeMapper = typeMapper;
   readonly IRemotableTessageSerializer _serializer = serializer;
   readonly ITessagesInFlightTracker _tessagesInFlightTracker = tessagesInFlightTracker;
   readonly string _remoteAddress = remoteAddress.AspNetAddress;
   readonly EndpointId _remoteEndpointId = remoteEndpointId;

   public async Task<TResult> PostAsync<TResult>(IAtMostOnceTommand<TResult> tommand)
   {
      var tessage = TransportTessage.OutGoing.Create(tommand, _typeMapper, _serializer);
      _tessagesInFlightTracker.SendingTessageOnTransport(tessage, _remoteEndpointId);
      return await _transportClient.PostAsync<TResult>(tessage, tommand, new Uri($"{_remoteAddress}{HttpConstants.Routes.Rpc.TommandWithResult}")).caf();
   }

   public async Task PostAsync(IAtMostOnceHypermediaTommand tommand)
   {
      var outGoingTessage = TransportTessage.OutGoing.Create(tommand, _typeMapper, _serializer);
      _tessagesInFlightTracker.SendingTessageOnTransport(outGoingTessage, _remoteEndpointId);
      await _transportClient.PostAsync(outGoingTessage, tommand, new Uri($"{_remoteAddress}{HttpConstants.Routes.Rpc.TommandNoResult}")).caf();
   }

   public async Task<TResult> GetAsync<TResult>(IRemotableTuery<TResult> tuery)
   {
      var tessage = TransportTessage.OutGoing.Create(tuery, _typeMapper, _serializer);
      _tessagesInFlightTracker.SendingTessageOnTransport(tessage, _remoteEndpointId);
      return await _transportClient.PostAsync<TResult>(tessage, tuery, new Uri($"{_remoteAddress}{HttpConstants.Routes.Rpc.Tuery}")).caf();
   }

   internal static async Task<(HttpApiClient, TessageTypesInternal.EndpointInformation)> BootstrapConnectionToEndpoint(IRemoteApiTransportClient remoteApiTransportClient, HttpEndPointAddress remoteAddress, ITypeMapper typeMapper, IRemotableTessageSerializer serializer, ITessagesInFlightTracker tessagesInFlightTracker)
   {
      var endpointInformationTuery = new TessageTypesInternal.EndpointInformationTuery();
      var endpointInformationTueryTessage = TransportTessage.OutGoing.Create(endpointInformationTuery, typeMapper, serializer);
      var endpointInformation = await remoteApiTransportClient.PostAsync<TessageTypesInternal.EndpointInformation>(endpointInformationTueryTessage, endpointInformationTuery, new Uri($"{remoteAddress.AspNetAddress}{HttpConstants.Routes.Rpc.Tuery}")).caf();
      return (new HttpApiClient(remoteApiTransportClient, remoteAddress, typeMapper, serializer, tessagesInFlightTracker, endpointInformation.Id), endpointInformation);
   }
}
