// ReSharper disable RedundantNameQualifier
// ReSharper disable UnusedTypeParameter
// ReSharper disable MemberHidesStaticFromOuterClass

using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Abstractions.Refactoring.Naming.Internal.Implementation;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Routing.Abstractions;
using Compze.Tessaging.Implementation.Transport.Infrastructure;
using Compze.Typermedia.HandlerRegistration;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Compze.Tessaging.Implementation.Abstractions;

// ReSharper disable once MemberCanBeInternal — Serialized across assemblies via Newtonsoft reflection
public static class TessageTypesInternal
{
#pragma warning disable CA1040 // Marker interface used for type-routing
   internal interface ITessage : IInternalInfrastructureTessage;
#pragma warning restore CA1040

   internal class EndpointInformationQuery : TessageTypesInternal.ITessage, IQuery<EndpointInformation>;

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

      internal EndpointInformation(IEnumerable<TypeId> handledRemoteTessageTypeIds, EndpointConfiguration configuration)
      {
         Id = configuration.Id;
         Name = configuration.Name;
         HandledTessageTypes = [..handledRemoteTessageTypeIds];
      }

      // ReSharper disable MemberCanBeInternal — Serialized across assemblies via Newtonsoft reflection
      public string Name { get; private set; }
      public EndpointId Id { get; private set; }
      public HashSet<TypeId> HandledTessageTypes { get; private set; }
      // ReSharper restore MemberCanBeInternal
   }

   internal class NetworkTopologyQuery : TessageTypesInternal.ITessage, IQuery<NetworkTopology>;

   internal class NetworkTopology
   {
      [Obsolete("Called by serializer", error: true)]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
      // ReSharper disable once UnusedMember.Global
      public NetworkTopology() {}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

      public NetworkTopology(IEnumerable<EndPointAddress> endpointAddresses) => EndpointAddresses = endpointAddresses.ToList();

      public IReadOnlyList<EndPointAddress> EndpointAddresses { get; private set; }
   }

   internal static void RegisterInfrastructureQueryHandlers(InfrastructureQueryRegistrarWithDependencyInjectionSupport registrar)
   {
      registrar.ForQuery((EndpointInformationQuery _, TypeMapper _, ITessageHandlerRegistry tessagingRegistry, ITypermediaHandlerRegistry typermediaRegistry, EndpointConfiguration configuration) =>
                            new EndpointInformation(tessagingRegistry.HandledRemoteTessageTypeIds().Concat(typermediaRegistry.HandledRemoteTypermediaTypeIds()), configuration));

      registrar.ForQuery((NetworkTopologyQuery _, IEndpointRegistry endpointRegistry) =>
                            new NetworkTopology(endpointRegistry.ServerEndpointAddresses));
   }
}
