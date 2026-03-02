using System;
using System.Threading.Tasks;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Serialization.Internal;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Internal;
using Compze.Threading.TasksCE;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation.Universal;

class TypermediaConnection : IDisposable
{
   public TessageTypesInternal.EndpointInformation EndpointInformation { get; private set; } = null!;
   public IRemoteApiEndpointClient ApiClient { get; private set; } = null!;

   readonly EndPointAddress _remoteAddress;
   readonly ITypeMapper _typeMapper;
   readonly IRemotableTessageSerializer _serializer;
   readonly ITransportMessagePoster _transportMessagePoster;

   public TypermediaConnection(
      EndPointAddress remoteAddress,
      ITypeMapper typeMapper,
      IRemotableTessageSerializer serializer,
      ITransportMessagePoster transportMessagePoster)
   {
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
   }

   public void Dispose() {}
}
