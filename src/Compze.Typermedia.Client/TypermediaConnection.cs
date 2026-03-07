using Compze.Core.Tessaging.Transport.Internal;
using Compze.Internals.Transport;

namespace Compze.Typermedia.Client;

class TypermediaConnection(EndPointAddress address, EndpointInformation endpointInformation)
{
   public EndPointAddress Address { get; } = address;
   public EndpointInformation EndpointInformation { get; } = endpointInformation;
}
