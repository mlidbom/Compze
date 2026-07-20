using Compze.Tessaging.Endpoints.Discovery;
using Compze.Tessaging.Internal.Transport.Advertisement;

namespace Compze.Tessaging.Typermedia.Client.Internal;

class TypermediaConnection(EndpointAddress address, EndpointInformation endpointInformation)
{
   public EndpointAddress Address { get; } = address;
   public EndpointInformation EndpointInformation { get; } = endpointInformation;
}
