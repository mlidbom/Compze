using System.Collections.Generic;
using Compze.Core.Tessaging.Hosting.Public;

namespace Compze.Tessaging.Implementation.Transport.Client.Routing.Abstractions;

public interface IEndpointRegistry
{
    IEnumerable<IEndpoint> ServerEndpoints { get; }
}