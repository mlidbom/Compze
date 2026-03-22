using Compze.TypeIdentifiers;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Hosting.Testing.Wiring;

public static class StructuralTypeMapperTestRegistrar
{
   /// <summary>
   /// Creates a <see cref="StructuralTypeMapper"/> pre-populated from all loaded assemblies
   /// with <c>[TypeMappings]</c> and registers it in DI. Test-only convenience.
   /// Production code should use <see cref="IStructuralTypeMapper.MapTypesFromAssemblyContaining{T}"/>
   /// on the endpoint builder's <c>TypeMapper</c>.
   /// </summary>
   public static IComponentRegistrar StructuralTypeMapperFromLoadedAssemblies(this IComponentRegistrar @this)
   {
      var mapper = new StructuralTypeMapper();
      mapper.MapTypesFromAllLoadedAssembliesWithTypeMappingsAttribute();
      return @this.Register(Singleton.For<IStructuralTypeMapper>().Instance(mapper));
   }
}
