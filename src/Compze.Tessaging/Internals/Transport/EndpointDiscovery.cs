using System.Text.Json.Serialization;
using Compze.TypeIdentifiers;
using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Internals.Transport;

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

   public string Name { get; }
   public EndpointId Id { get; }
   public HashSet<string> HandledTessageTypes { get; }
}

///<summary>Contributes an endpoint's advertised remotable tessage type-ids to its one <see cref="EndpointInformation"/><br/>
/// advertisement: each communication style's handler registry contributes the remotable types it serves.</summary>
///<remarks>An interim seam: when the <c>TessageHandlerRoster</c> lands (see<br/>
/// <c>dev_docs/TODO/WIP/Tessaging/tessaging-target-design.md</c>) the one roster is the advertisement's single source and this<br/>
/// contribution set dies with the feature machinery.</remarks>
interface IEndpointAdvertisementContributor
{
   ISet<TypeId> AdvertisedRemoteTessageTypeIds();
}

static class EndpointInformationQueryRegistration
{
   internal static void RegisterQueryHandlers(EndpointDiscoveryQueryRegistrarWithDependencyInjectionSupport registrar) =>
      registrar.ForQuery((EndpointInformationQuery _, IComponentSet<IEndpointAdvertisementContributor> contributors, EndpointConfiguration configuration) =>
                            new EndpointInformation([..contributors.SelectMany(contributor => contributor.AdvertisedRemoteTessageTypeIds())], configuration));
}
