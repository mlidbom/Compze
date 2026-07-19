using Compze.DependencyInjection.Abstractions;
using Compze.Tests.Common;
using Compze.TypeIdentifiers.DependencyInjection;

namespace Compze.Tests.Integration;

public static class IntegrationTestTypeMappings
{
   /// <summary>Requires the shared test domain (Compze.Tests.Common) plus this assembly's own type identity.</summary>
   public static IComponentRegistrar RequireIntegrationTestTypeMappings(this IComponentRegistrar @this) =>
      @this.RequireCommonTestTypeMappings()
           .RequireMappedTypesFromAssemblyContaining<AssemblyTypeMapper>();
}
