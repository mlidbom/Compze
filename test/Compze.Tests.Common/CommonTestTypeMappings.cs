using Compze.DependencyInjection.Abstractions;
using Compze.TypeIdentifiers.DependencyInjection;

namespace Compze.Tests.Common;

public static class CommonTestTypeMappings
{
   /// <summary>Requires the type identity of the Compze.Tests.Common assembly — the shared test domain.</summary>
   public static IComponentRegistrar RequireCommonTestTypeMappings(this IComponentRegistrar @this)
      => @this.RequireMappedTypesFromAssemblyContaining<AssemblyTypeMapper>();
}
