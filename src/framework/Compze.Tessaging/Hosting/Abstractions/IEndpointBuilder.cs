using System;
using Compze.Abstractions.Internal.Refactoring.Naming;
using Compze.DependencyInjection;
using Compze.Tessaging.Hosting.Implementation;

namespace Compze.Tessaging.Hosting.Abstractions;

public interface IEndpointBuilder : IDisposable
{
    IDependencyInjectionContainer Container { get; }
    ITypeMappingRegistrar TypeMapper { get; }
    EndpointConfiguration Configuration { get; }
    MessageHandlerRegistrarWithDependencyInjectionSupport RegisterHandlers { get; }
}
