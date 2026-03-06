using Compze.Core.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Core.Tessaging.Hosting.Public;

//Todo, we should have a testing version of this that can register the current test's sql layer etc.
public interface IEndpointBuilder
{
    IDependencyInjectionContainer Container { get; }
    EndpointConfiguration Configuration { get; }
    TessageHandlerRegistrarWithDependencyInjectionSupport RegisterHandlers { get; }
}
