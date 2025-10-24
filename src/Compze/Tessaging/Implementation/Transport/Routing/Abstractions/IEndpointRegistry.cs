using System.Collections.Generic;
using Compze.Tessaging.Hosting.Abstractions.Transport;

namespace Compze.Tessaging.Implementation.Transport.Routing.Abstractions;

interface IEndpointRegistry
{
    IEnumerable<HttpEndPointAddress> ServerEndpoints { get; }
}