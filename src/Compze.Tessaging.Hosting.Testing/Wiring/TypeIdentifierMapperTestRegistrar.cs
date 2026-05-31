using System.Reflection;
using Compze.TypeIdentifiers;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Hosting.Testing.Wiring;

public static class TypeIdentifierMapperTestRegistrar
{
   /// <summary>
   /// Creates a <see cref="TypeMapper"/> populated from every loaded assembly that carries an
   /// <see cref="AssemblyTypeMapperAttribute"/>, and registers it in DI. Test-only convenience.
   /// </summary>
   public static IComponentRegistrar TypeIdentifierMapperFromLoadedAssemblies(this IComponentRegistrar @this)
   {
      var mapper = new TypeMapper();
      mapper.MapAllLoadedAssembliesWithTypeMappings();
      return @this.Register(Singleton.For<ITypeMapper>().Instance(mapper))
                  .Register(Singleton.For<ITypeMap>().Instance(mapper));
   }

   /// <summary>
   /// Test-only: registers every loaded assembly that carries an <see cref="AssemblyTypeMapperAttribute"/> onto the
   /// mapper. Scanning the whole AppDomain is acceptable in a test host that controls exactly what is loaded — it is
   /// the test-side replacement for the old production auto-discovery. Production registers the framework's mappings
   /// explicitly via <c>MapCompzeFrameworkTypes()</c> and each endpoint's own domain mappings per endpoint.
   /// </summary>
   public static void MapAllLoadedAssembliesWithTypeMappings(this ITypeMapper mapper)
   {
      foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(it => it.GetCustomAttribute<AssemblyTypeMapperAttribute>() != null))
         mapper.MapTypesFromAssembly(assembly);
   }
}
