using Compze.DependencyInjection.Abstractions;
using Compze.Tests.Integration;
using Compze.TypeIdentifiers.DependencyInjection;

namespace Compze.Tests.Performance.Internals;

public static class PerformanceTestTypeMappings
{
   /// <summary>Requires the shared and integration test domains plus this assembly's own type identity.</summary>
   public static IComponentRegistrar RequirePerformanceTestTypeMappings(this IComponentRegistrar @this) =>
      @this.RequireIntegrationTestTypeMappings()
           .RequireMappedTypesFromAssemblyContaining<AssemblyTypeMapper>();
}
