using System;
using Compze.Tessaging.Hosting.Abstractions.MessageHandling.Registration;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Hosting.Abstractions;

//Todo, we should have a testing version of this that can register the current test's sql layer etc.
public interface IEndpointBuilder : IDisposable
{
    IDependencyInjectionContainer Container { get; }
    EndpointConfiguration Configuration { get; }
    MessageHandlerRegistrarWithDependencyInjectionSupport RegisterHandlers { get; }
}
