using Compze.TypeIdentifiers;
using Compze.Abstractions.Tessaging.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Core.Tessaging.Transport.Internal;

namespace Compze.Internals.Transport;

public class EndpointInformationQuery : IQuery<EndpointInformation>;

// ReSharper disable once MemberCanBeInternal — Serialized across assemblies via Newtonsoft reflection
public class EndpointInformation
{
   [Obsolete("Called by serializer", error: true)]
   // ReSharper disable MemberCanBeInternal — Called by serializer via reflection
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
   // ReSharper disable once UnusedMember.Global
   public EndpointInformation() {}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
   // ReSharper restore MemberCanBeInternal

   public EndpointInformation(IEnumerable<StructuralTypeId> handledRemoteTessageTypeIds, EndpointConfiguration configuration)
   {
      Id = configuration.Id;
      Name = configuration.Name;
      HandledTessageTypes = handledRemoteTessageTypeIds.Select(id => id.StringRepresentation).ToHashSet();
   }

   // ReSharper disable MemberCanBeInternal — Serialized across assemblies via Newtonsoft reflection
   public string Name { get; private set; }
   public EndpointId Id { get; private set; }
   public HashSet<string> HandledTessageTypes { get; private set; }
   // ReSharper restore MemberCanBeInternal
}

public class NetworkTopologyQuery : IQuery<NetworkTopology>;

public class NetworkTopology
{
   [Obsolete("Called by serializer", error: true)]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
   // ReSharper disable once UnusedMember.Global
   public NetworkTopology() {}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

   public NetworkTopology(IEnumerable<EndPointAddress> endpointAddresses) => EndpointAddresses = endpointAddresses.ToList();

   public IReadOnlyList<EndPointAddress> EndpointAddresses { get; private set; }
}
