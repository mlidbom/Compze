using System.Collections.Generic;
using Compze.Hosting.Abstractions;

namespace Compze.Tessaging.Hosting.Implementation.Abstractions;

interface IEndpointRegistry
{
    IEnumerable<EndPointAddress> ServerEndpoints { get; }
}