using Compze.DependencyInjection.Abstractions;
using Compze.Teventive.Taggregates.Tevents.Public;
using Compze.TypeIdentifiers.DependencyInjection;

namespace Compze.Tests.Common.Wiring;

public static class TypeIdentifierMapperTestRegistrar
{
   /// <summary>
   /// Requires the type identity a plain testing container needs. A container that is not an endpoint composes no engine
   /// and no store, so nothing else declares the Teventive type hierarchy for it — that one requirement is what this adds
   /// on top of whatever the container's own components declare.
   /// </summary>
   /// <remarks>
   /// Tests declare their domain explicitly, exactly as a production composition does, so a test that forgets a type fails
   /// the same way the real application would, and there is no AppDomain-wide scan.
   /// </remarks>
   public static IComponentRegistrar TypeIdentifierMapper(this IComponentRegistrar @this, Action<IComponentRegistrar> declareRequiredDomainTypeMappings)
   {
      @this.RequireMappedTypesFromAssemblyContaining<ITaggregateTevent>();
      declareRequiredDomainTypeMappings(@this);
      return @this;
   }
}
