using System.Text.Json.Serialization;
using Compze.Tessaging._private.Transport.Advertisement;
using Compze.Tessaging.Endpoints;
using Compze.TypeIdentifiers;

// ReSharper disable UnusedAutoPropertyAccessor.Global serilization requires it

namespace Compze.Tessaging._internal.Transport.Advertisement;

///<summary>The answer to endpoint discovery: who the endpoint is and its one advertisement — every remotable tessage type it<br/>
/// serves, of every kind (tevent subscriptions, tommands, tueries, typermedia tommands), as canonical type-id strings.<br/>
/// A plain wire shape serialized by the fixed <see cref="EndpointInformationQuerySerializer"/> format.</summary>
class EndpointInformation
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
