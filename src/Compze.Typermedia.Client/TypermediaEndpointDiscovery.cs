using System.Text.Json.Serialization;
using Compze.TypeIdentifiers;
using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Internals.Transport;
using Compze.Typermedia.HandlerRegistration;

namespace Compze.Typermedia.Client;

class TypermediaEndpointInformationQuery : ITuery<TypermediaEndpointInformation>;

///<summary>Typermedia's answer to endpoint discovery: who the endpoint is and which typermedia tessage types it serves —<br/>
/// a plain wire shape serialized by the fixed <see cref="EndpointDiscoverySerializer"/> format.</summary>
public class TypermediaEndpointInformation
{
   internal TypermediaEndpointInformation(IEnumerable<TypeId> handledTypermediaTypeIds, EndpointConfiguration configuration)
      : this(configuration.Name, configuration.Id, handledTypermediaTypeIds.Select(id => id.CanonicalString).ToHashSet()) {}

   [JsonConstructor]
   public TypermediaEndpointInformation(string name, EndpointId id, HashSet<string> handledTypermediaTypes)
   {
      Name = name;
      Id = id;
      HandledTypermediaTypes = handledTypermediaTypes;
   }

   public string Name { get; }
   public EndpointId Id { get; }
   public HashSet<string> HandledTypermediaTypes { get; }
}

public static class TypermediaEndpointDiscoveryQueryRegistration
{
   public static void RegisterQueryHandlers(EndpointDiscoveryQueryRegistrarWithDependencyInjectionSupport registrar) =>
      registrar.ForQuery((TypermediaEndpointInformationQuery _, ITypermediaHandlerRegistry typermediaRegistry, EndpointConfiguration configuration) =>
                            new TypermediaEndpointInformation(typermediaRegistry.HandledRemoteTypermediaTypeIds(), configuration));
}
