using System.Text.Json.Serialization;
using Compze.Tessaging.Abstractions.TessageTypes;
using Compze.Tessaging.Endpoints;
using Compze.TypeIdentifiers;

// ReSharper disable UnusedAutoPropertyAccessor.Global serilization requires it

namespace Compze.Tessaging.Transport.Discovery;

///<summary>The endpoint-discovery query "who are you, and which remotable tessage types do you serve?" — the one question a<br/>
/// connecting endpoint's router asks to learn the identity behind an address and build its routes, for every tessage kind at<br/>
/// once. Every transport-speaking endpoint serves it.</summary>
class EndpointInformationQuery : ITuery<EndpointInformation>;

///<summary>The answer to endpoint discovery: who the endpoint is and its one advertisement — every remotable tessage type it<br/>
/// serves, of every kind (tevent subscriptions, tommands, tueries, typermedia tommands), as canonical type-id strings.<br/>
/// A plain wire shape serialized by the fixed <see cref="EndpointDiscoverySerializer"/> format.</summary>
public class EndpointInformation
{
   internal EndpointInformation(IEnumerable<TypeId> advertisedTessageTypeIds, EndpointConfiguration configuration)
      : this(configuration.Name, configuration.Id, [..advertisedTessageTypeIds.Select(id => id.CanonicalString)]) {}

   [JsonConstructor]
   public EndpointInformation(string name, EndpointId id, HashSet<string> handledTessageTypes)
   {
      Name = name;
      Id = id;
      HandledTessageTypes = handledTessageTypes;
   }

   public string Name { get; private set; }
   public EndpointId Id { get; private set;}
   public HashSet<string> HandledTessageTypes { get; private set;}
}

