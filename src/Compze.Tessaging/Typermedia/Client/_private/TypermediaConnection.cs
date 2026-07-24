using Compze.Tessaging.Endpoints.Discovery;

using Compze.Tessaging._private.Transport.Advertisement;

namespace Compze.Tessaging.Typermedia.Client._private;

class TypermediaConnection(EndpointAddress address, EndpointInformation endpointInformation)
{
   public EndpointAddress Address { get; } = address;
   public EndpointInformation EndpointInformation { get; } = endpointInformation;
}
