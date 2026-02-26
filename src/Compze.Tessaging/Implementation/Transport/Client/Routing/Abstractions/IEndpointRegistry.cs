using System.Collections.Generic;
using Compze.Core.Tessaging.Transport.Internal;

namespace Compze.Tessaging.Implementation.Transport.Client.Routing.Abstractions;

public interface IEndpointRegistry
{
    IEnumerable<EndPointAddress> ServerEndpointAddresses { get; }
}