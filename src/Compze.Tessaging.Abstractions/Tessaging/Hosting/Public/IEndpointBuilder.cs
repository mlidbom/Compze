using Compze.TypeIdentifiers;
using Compze.Abstractions.Tessaging.Hosting.Public;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Abstractions.Tessaging.Hosting.Public;

//Todo, we should have a testing version of this that can register the current test's sql layer etc.
public interface IEndpointBuilder
{
    ITypeMapper TypeMapper { get; }
    IComponentRegistrar Registrar { get; }
    EndpointConfiguration Configuration { get; }
}
