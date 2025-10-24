using System.Collections.Generic;
using Compze.Tessaging.Hosting.Abstractions.Transport;

namespace Compze.Tessaging.Hosting.Implementation.Abstractions;

interface IEndpointRegistry
{
    IEnumerable<HttpEndPointAddress> ServerEndpoints { get; }
}