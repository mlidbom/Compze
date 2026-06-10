using Compze.TypeIdentifiers;
using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;

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

   public EndpointInformation(IEnumerable<TypeId> handledRemoteTessageTypeIds, EndpointConfiguration configuration)
   {
      Id = configuration.Id;
      Name = configuration.Name;
      HandledTessageTypes = handledRemoteTessageTypeIds.Select(id => id.CanonicalString).ToHashSet();
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

   public NetworkTopology(IEnumerable<EndpointAddress> endpointAddresses) => EndpointAddresses = endpointAddresses.ToList();

   public IReadOnlyList<EndpointAddress> EndpointAddresses { get; private set; }
}
