using System.Collections.Generic;
using Compze.Core.Tessaging.Hosting.Public;

namespace Compze.Tessaging.Implementation.Transport.Client.Routing.Abstractions;

interface IEndpointRegistry
{
    IEnumerable<IEndpoint> ServerEndpoints { get; }
}