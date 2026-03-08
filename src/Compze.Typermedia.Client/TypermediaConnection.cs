using Compze.Core.Tessaging.Transport.Internal;

namespace Compze.Typermedia.Client;

class TypermediaConnection(EndPointAddress address, TypermediaEndpointInformation endpointInformation)
{
   public EndPointAddress Address { get; } = address;
   public TypermediaEndpointInformation EndpointInformation { get; } = endpointInformation;
}
