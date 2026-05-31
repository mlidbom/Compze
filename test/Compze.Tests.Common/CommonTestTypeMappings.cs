using Compze.TypeIdentifiers;

namespace Compze.Tests.Common;

public static class CommonTestTypeMappings
{
   /// <summary>Registers the type mappings declared by the Compze.Tests.Common assembly (the shared test domain).</summary>
   public static void RegisterCommonTestTypeMappings(this ITypeMapper mapper)
      => mapper.MapTypesFromAssemblyContaining<AssemblyTypeMapper>();
}
