using Compze.Abstractions.Tessaging.Hosting.Public;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Abstractions.Tessaging.Hosting.Public;

//Todo, we should have a testing version of this that can register the current test's sql layer etc.
public interface IEndpointBuilder
{
    IDependencyInjectionContainer Container { get; }
    EndpointConfiguration Configuration { get; }
    TessageHandlerRegistrarWithDependencyInjectionSupport RegisterTessagingHandlers { get; }
}
