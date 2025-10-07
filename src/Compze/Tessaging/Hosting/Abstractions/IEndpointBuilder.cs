using System;
using Compze.Abstractions.Internal.Refactoring.Naming;
using Compze.Utilities.DependencyInjection;

namespace Compze.Tessaging.Hosting.Abstractions;

public interface IEndpointBuilder : IDisposable
{
    IDependencyInjectionContainer Container { get; }
    EndpointConfiguration Configuration { get; }
    MessageHandlerRegistrarWithDependencyInjectionSupport RegisterHandlers { get; }
}
