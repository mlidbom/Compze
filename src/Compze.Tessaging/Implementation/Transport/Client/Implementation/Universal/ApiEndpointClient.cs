using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Abstractions.Serialization.Internal;
using Compze.Abstractions.Tessaging.Public;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Internals.SystemCE.Core.ThreadingCE.TasksCE;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Internal;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation.Universal;

class ApiEndpointClient(
   ITransportMessagePoster transportMessagePoster,
   EndPointAddress remoteAddress,
   ITypeMapper typeMapper,
   IRemotableTessageSerializer serializer) : IRemoteApiEndpointClient
{
   readonly ITransportMessagePoster _transportMessagePoster = transportMessagePoster;
   readonly ITypeMapper _typeMapper = typeMapper;
   readonly IRemotableTessageSerializer _serializer = serializer;
   readonly EndPointAddress _remoteAddress = remoteAddress;

   public async Task<TResult> PostAsync<TResult>(IAtMostOnceTommand<TResult> typermediaTommand)
   {
      var tessage = TransportTessage.OutGoing.Create(typermediaTommand, _typeMapper, _serializer);
      return await _transportMessagePoster.PostAsync<TResult>(tessage, _remoteAddress).caf();
   }

   public async Task PostAsync(IAtMostOnceTypermediaTommand tommand)
   {
      var outGoingTessage = TransportTessage.OutGoing.Create(tommand, _typeMapper, _serializer);
      await _transportMessagePoster.PostAsync(outGoingTessage, _remoteAddress).caf();
   }

   public async Task<TResult> GetAsync<TResult>(IRemotableTuery<TResult> tuery)
   {
      var tessage = TransportTessage.OutGoing.Create(tuery, _typeMapper, _serializer);
      return await _transportMessagePoster.PostAsync<TResult>(tessage, _remoteAddress).caf();
   }

   internal static async Task<(ApiEndpointClient, TessageTypesInternal.EndpointInformation)> BootstrapConnectionToEndpoint(ITransportMessagePoster transportMessagePoster,
                                                                                                                          EndPointAddress remoteAddress,
                                                                                                                          ITypeMapper typeMapper,
                                                                                                                          IRemotableTessageSerializer serializer)
   {
      var endpointInformationTuery = new TessageTypesInternal.EndpointInformationTuery();
      var endpointInformationTueryTessage = TransportTessage.OutGoing.Create(endpointInformationTuery, typeMapper, serializer);
      var endpointInformation = await transportMessagePoster
                                     .PostAsync<TessageTypesInternal.EndpointInformation>(
                                         endpointInformationTueryTessage,
                                         remoteAddress).caf();
      return (new ApiEndpointClient(transportMessagePoster, remoteAddress, typeMapper, serializer), endpointInformation);
   }
}
