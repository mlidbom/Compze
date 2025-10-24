using System.Collections.Generic;
using Compze.Abstractions.Tessaging.Transport.Internal;

namespace Compze.Tessaging.Implementation.Transport.Routing.Abstractions;

interface IEndpointRegistry
{
    IEnumerable<HttpEndPointAddress> ServerEndpoints { get; }
}