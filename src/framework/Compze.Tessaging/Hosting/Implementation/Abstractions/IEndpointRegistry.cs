using System.Collections.Generic;

namespace Compze.Tessaging.Hosting.Implementation.Abstractions;

interface IEndpointRegistry
{
    IEnumerable<EndPointAddress> ServerEndpoints { get; }
}