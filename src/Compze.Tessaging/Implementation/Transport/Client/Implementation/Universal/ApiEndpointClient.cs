using System.Threading.Tasks;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Serialization.Internal;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Public;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Internal;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation.Universal;

public class ApiEndpointClient(
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
      return await _transportMessagePoster.PostAsync<TResult>(tessage, typermediaTommand, _remoteAddress).caf();
   }

   public async Task PostAsync(IAtMostOnceTypermediaTommand tommand)
   {
      var outGoingTessage = TransportTessage.OutGoing.Create(tommand, _typeMapper, _serializer);
      await _transportMessagePoster.PostAsync(outGoingTessage, tommand, _remoteAddress).caf();
   }

   public async Task<TResult> GetAsync<TResult>(IRemotableTuery<TResult> tuery)
   {
      var tessage = TransportTessage.OutGoing.Create(tuery, _typeMapper, _serializer);
      return await _transportMessagePoster.PostAsync<TResult>(tessage, tuery, _remoteAddress).caf();
   }

   public static async Task<(ApiEndpointClient, TessageTypesInternal.EndpointInformation)> BootstrapConnectionToEndpoint(ITransportMessagePoster transportMessagePoster,
                                                                                                                          EndPointAddress remoteAddress,
                                                                                                                          ITypeMapper typeMapper,
                                                                                                                          IRemotableTessageSerializer serializer)
   {
      var endpointInformationTuery = new TessageTypesInternal.EndpointInformationTuery();
      var endpointInformationTueryTessage = TransportTessage.OutGoing.Create(endpointInformationTuery, typeMapper, serializer);
      var endpointInformation = await transportMessagePoster
                                     .PostAsync<TessageTypesInternal.EndpointInformation>(
                                         endpointInformationTueryTessage,
                                         endpointInformationTuery,
                                         remoteAddress).caf();
      return (new ApiEndpointClient(transportMessagePoster, remoteAddress, typeMapper, serializer), endpointInformation);
   }
}
