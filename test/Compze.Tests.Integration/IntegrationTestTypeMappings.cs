using Compze.TypeIdentifiers;
using Compze.Tests.Common;

namespace Compze.Tests.Integration;

public static class IntegrationTestTypeMappings
{
   /// <summary>Registers the shared test domain (Compze.Tests.Common) plus this assembly's own type mappings.</summary>
   public static void RegisterIntegrationTestTypeMappings(this ITypeMapper mapper)
   {
      mapper.RegisterCommonTestTypeMappings();
      mapper.MapTypesFromAssemblyContaining<Compze.Tests.Integration.AssemblyTypeMapper>();
   }
}
