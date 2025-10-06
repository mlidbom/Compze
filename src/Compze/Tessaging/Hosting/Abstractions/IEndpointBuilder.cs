using System;
using Compze.Abstractions.Internal.Refactoring.Naming;
using Compze.DependencyInjection;

namespace Compze.Hosting.Abstractions;

public interface IEndpointBuilder : IDisposable
{
    IDependencyInjectionContainer Container { get; }
    ITypeMappingRegistrar TypeMapper { get; }
    EndpointConfiguration Configuration { get; }
    MessageHandlerRegistrarWithDependencyInjectionSupport RegisterHandlers { get; }
}
