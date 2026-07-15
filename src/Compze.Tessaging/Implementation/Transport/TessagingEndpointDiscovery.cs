using System.Text.Json.Serialization;
using Compze.TypeIdentifiers;
using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Internals.Transport;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;

namespace Compze.Tessaging.Implementation.Transport;

///<summary>The tessaging endpoint-discovery query "who are you, and which remotable tessage types do you handle?" — what a<br/>
/// connecting endpoint's tessaging router asks to learn the identity behind an address and build its tommand and tevent routes.</summary>
class TessagingEndpointInformationQuery : ITuery<TessagingEndpointInformation>;

///<summary>Tessaging's answer to endpoint discovery: who the endpoint is and which remotable tessage types it handles —<br/>
/// a plain wire shape serialized by the fixed <see cref="EndpointDiscoverySerializer"/> format.</summary>
public class TessagingEndpointInformation
{
   internal TessagingEndpointInformation(IEnumerable<TypeId> handledRemoteTessageTypeIds, EndpointConfiguration configuration)
      : this(configuration.Name, configuration.Id, [..handledRemoteTessageTypeIds.Select(id => id.CanonicalString)]) {}

   [JsonConstructor]
   public TessagingEndpointInformation(string name, EndpointId id, HashSet<string> handledTessageTypes)
   {
      Name = name;
      Id = id;
      HandledTessageTypes = handledTessageTypes;
   }

   public string Name { get; }
   public EndpointId Id { get; }
   public HashSet<string> HandledTessageTypes { get; }
}

static class TessagingEndpointDiscoveryQueryRegistration
{
   internal static void RegisterQueryHandlers(EndpointDiscoveryQueryRegistrarWithDependencyInjectionSupport registrar) =>
      registrar.ForQuery((TessagingEndpointInformationQuery _, ITessageHandlerRegistry tessagingRegistry, EndpointConfiguration configuration) =>
                            new TessagingEndpointInformation(tessagingRegistry.HandledRemoteTessageTypeIds(), configuration));
}
