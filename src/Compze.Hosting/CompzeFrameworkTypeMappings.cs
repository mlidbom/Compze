using Compze.TypeIdentifiers;
using Compze.Abstractions.Tessaging.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Internals.Transport;
using Compze.Typermedia.Client;

namespace Compze.Hosting;

/// <summary>
/// Registers the Compze framework's own type mappings — the infrastructure tessage and typermedia types that every
/// endpoint needs regardless of which features it enables. This is the explicit, curated replacement for scanning
/// the whole AppDomain: feature-specific mappings (e.g. the TeventStore) are registered by the corresponding
/// <c>RegisterX()</c> builder extension, and an endpoint's own domain mappings by the endpoint itself.
/// </summary>
public static class CompzeFrameworkTypeMappings
{
   public static void MapCompzeFrameworkTypes(this ITypeMapper mapper)
   {
      mapper.MapTypesFromAssemblyContaining<IExactlyOnceTevent>();            // Compze.Abstractions
      mapper.MapTypesFromAssemblyContaining<ITaggregateTevent>();            // Compze.Core
      mapper.MapTypesFromAssemblyContaining<EndpointInformationQuery>();     // Compze.Internals.Transport
      mapper.MapTypesFromAssemblyContaining<TypermediaEndpointInformation>(); // Compze.Typermedia.Client
   }
}
