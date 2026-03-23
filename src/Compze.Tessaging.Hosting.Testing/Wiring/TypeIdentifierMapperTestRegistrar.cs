using Compze.TypeIdentifiers;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Hosting.Testing.Wiring;

public static class TypeIdentifierMapperTestRegistrar
{
   /// <summary>
   /// Creates a <see cref="TypeMapper"/> pre-populated from all loaded assemblies
   /// with <c>[TypeMappings]</c> and registers it in DI. Test-only convenience.
   /// Production code should use <see cref="ITypeMapper.MapTypesFromAssemblyContaining{T}"/>
   /// on the endpoint builder's <c>TypeMapper</c>.
   /// </summary>
   public static IComponentRegistrar TypeIdentifierMapperFromLoadedAssemblies(this IComponentRegistrar @this)
   {
      var mapper = new TypeMapper();
      mapper.MapTypesFromAllLoadedAssembliesWithTypeMappingsAttribute();
      return @this.Register(Singleton.For<ITypeMapper>().Instance(mapper));
   }
}
