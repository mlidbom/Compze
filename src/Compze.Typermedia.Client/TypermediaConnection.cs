using Compze.Abstractions.Hosting.Public;

namespace Compze.Typermedia.Client;

class TypermediaConnection(EndpointAddress address, TypermediaEndpointInformation endpointInformation)
{
   public EndpointAddress Address { get; } = address;
   public TypermediaEndpointInformation EndpointInformation { get; } = endpointInformation;
}
