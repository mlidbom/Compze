using System.Collections.Generic;
using Compze.Tessaging.Hosting.Abstractions;

namespace Compze.Tessaging.Hosting.Implementation.Abstractions;

interface IEndpointRegistry
{
    IEnumerable<EndPointAddress> ServerEndpoints { get; }
}