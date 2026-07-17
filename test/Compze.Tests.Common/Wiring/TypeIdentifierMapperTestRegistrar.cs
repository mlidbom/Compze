using Compze.Abstractions.Tessaging.Public;
using Compze.Tessaging.Internals.Transport;
using Compze.TypeIdentifiers;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Teventive.Taggregates.Tevents.Public;

namespace Compze.Tests.Common.Wiring;

public static class TypeIdentifierMapperTestRegistrar
{
   /// <summary>
   /// Registers a <see cref="TypeMapper"/> in DI, populated with the Compze framework's own mappings plus the
   /// caller's domain mappings. Tests register their domain explicitly — exactly as a production endpoint does —
   /// so a test that forgets to register a type fails the same way the real application would, and there is no
   /// AppDomain-wide scan.
   /// </summary>
   public static IComponentRegistrar TypeIdentifierMapper(this IComponentRegistrar @this, Action<ITypeMapper> registerDomainTypeMappings)
   {
      var mapper = new TypeMapper();
      mapper.MapTypesFromAssemblyContaining<IExactlyOnceTevent>();            // Compze.Abstractions
      mapper.MapTypesFromAssemblyContaining<ITaggregateTevent>();             // Compze.Teventive — the Teventive type hierarchy
      mapper.MapTypesFromAssemblyContaining<EndpointInformation>();           // Compze.Tessaging — the endpoint-discovery types
      registerDomainTypeMappings(mapper);
      return @this.Register(Singleton.For<ITypeMapper>().Instance(mapper))
                  .Register(Singleton.For<ITypeMap>().Instance(mapper));
   }
}
