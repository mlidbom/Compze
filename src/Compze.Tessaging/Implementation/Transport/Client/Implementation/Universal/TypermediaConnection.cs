using Compze.Core.Tessaging.Transport.Internal;
using Compze.Tessaging.Implementation.Abstractions;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation.Universal;

class TypermediaConnection(EndPointAddress address, TessageTypesInternal.EndpointInformation endpointInformation)
{
   public EndPointAddress Address { get; } = address;
   public TessageTypesInternal.EndpointInformation EndpointInformation { get; } = endpointInformation;
}
