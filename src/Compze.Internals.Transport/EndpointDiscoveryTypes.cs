using System.Text.Json.Serialization;
using Compze.TypeIdentifiers;
using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;

namespace Compze.Internals.Transport;

///<summary>The endpoint-discovery query "who are you, and which remotable tessage types do you handle?" — what a connecting<br/>
/// endpoint's router asks to learn the identity behind an address and build its tommand and tevent routes.</summary>
public class EndpointInformationQuery : IQuery<EndpointInformation>;

public class EndpointInformation
{
   public EndpointInformation(IEnumerable<TypeId> handledRemoteTessageTypeIds, EndpointConfiguration configuration)
      : this(configuration.Name, configuration.Id, [..handledRemoteTessageTypeIds.Select(id => id.CanonicalString)]) {}

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

///<summary>The endpoint-discovery query "whose addresses do you know?" — lets an endpoint that can reach one member of the<br/>
/// network learn the whole membership from it.</summary>
public class NetworkTopologyQuery : IQuery<NetworkTopology>;

public class NetworkTopology
{
   public NetworkTopology(IEnumerable<EndpointAddress> endpointAddresses) => EndpointAddresses = [..endpointAddresses];

   public IReadOnlyList<EndpointAddress> EndpointAddresses { get; }
}
