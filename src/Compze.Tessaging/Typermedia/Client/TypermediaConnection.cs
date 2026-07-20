using Compze.Tessaging.Internals.Transport;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.Tessaging.Internals.Transport.Discovery;

namespace Compze.Tessaging.Typermedia.Client;

class TypermediaConnection(EndpointAddress address, EndpointInformation endpointInformation)
{
   public EndpointAddress Address { get; } = address;
   public EndpointInformation EndpointInformation { get; } = endpointInformation;
}
