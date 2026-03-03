using Compze.Core.Tessaging.Transport.Internal;

namespace Compze.Tessaging.Implementation.Transport.Client.Routing.Abstractions;

interface IEndpointRegistry
{
    IEnumerable<EndPointAddress> ServerEndpointAddresses { get; }
}