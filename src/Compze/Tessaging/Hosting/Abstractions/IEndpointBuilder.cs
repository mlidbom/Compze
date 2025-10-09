using System;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Hosting.Abstractions;

public interface IEndpointBuilder : IDisposable
{
    IDependencyInjectionContainer Container { get; }
    EndpointConfiguration Configuration { get; }
    MessageHandlerRegistrarWithDependencyInjectionSupport RegisterHandlers { get; }
}
