using Compze.TypeIdentifiers;
using Compze.Tests.Integration;

namespace Compze.Tests.Performance.Internals;

public static class PerformanceTestTypeMappings
{
   /// <summary>Registers the shared + integration test domains plus this assembly's own type mappings.</summary>
   public static void RegisterPerformanceTestTypeMappings(this ITypeMapper mapper)
   {
      mapper.RegisterIntegrationTestTypeMappings();
      mapper.MapTypesFromAssemblyContaining<Compze.Tests.Performance.Internals.AssemblyTypeMapper>();
   }
}
