using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Abstractions.Tessaging.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Internals.Transport;
using Compze.Typermedia.HandlerRegistration;

namespace Compze.Typermedia.Client;

class TypermediaEndpointInformationQuery : IQuery<TypermediaEndpointInformation>;

// ReSharper disable once MemberCanBeInternal — Serialized across assemblies via Newtonsoft reflection
public class TypermediaEndpointInformation
{
   [Obsolete("Called by serializer", error: true)]
   // ReSharper disable MemberCanBeInternal — Called by serializer via reflection
#pragma warning disable CS8618
   // ReSharper disable once UnusedMember.Global
   public TypermediaEndpointInformation() {}
#pragma warning restore CS8618
   // ReSharper restore MemberCanBeInternal

   internal TypermediaEndpointInformation(IEnumerable<TypeId> handledTypermediaTypeIds, EndpointConfiguration configuration)
   {
      Id = configuration.Id;
      Name = configuration.Name;
      HandledTypermediaTypes = [..handledTypermediaTypeIds];
   }

   // ReSharper disable MemberCanBeInternal — Serialized across assemblies via Newtonsoft reflection
   public string Name { get; private set; }
   public EndpointId Id { get; private set; }
   public HashSet<TypeId> HandledTypermediaTypes { get; private set; }
   // ReSharper restore MemberCanBeInternal
}

public static class TypermediaInfrastructureQueryRegistration
{
   public static void RegisterQueryHandlers(InfrastructureQueryRegistrarWithDependencyInjectionSupport registrar) =>
      registrar.ForQuery((TypermediaEndpointInformationQuery _, ITypermediaHandlerRegistry typermediaRegistry, EndpointConfiguration configuration) =>
                            new TypermediaEndpointInformation(typermediaRegistry.HandledRemoteTypermediaTypeIds(), configuration));
}
