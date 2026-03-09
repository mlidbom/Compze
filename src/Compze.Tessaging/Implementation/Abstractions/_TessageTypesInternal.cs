// ReSharper disable RedundantNameQualifier
// ReSharper disable UnusedTypeParameter
// ReSharper disable MemberHidesStaticFromOuterClass

using Compze.Abstractions.Tessaging.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Routing.Abstractions;
using Compze.Internals.Transport;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Compze.Tessaging.Implementation.Abstractions;

// ReSharper disable once MemberCanBeInternal — Serialized across assemblies via Newtonsoft reflection
public static class TessageTypesInternal
{
#pragma warning disable CA1040 // Marker interface used for type-routing
   internal interface ITessage : IInternalInfrastructureTessage;
#pragma warning restore CA1040

   internal static void RegisterInfrastructureQueryHandlers(InfrastructureQueryRegistrarWithDependencyInjectionSupport registrar)
   {
      registrar.ForQuery((EndpointInformationQuery _, ITessageHandlerRegistry tessagingRegistry, EndpointConfiguration configuration) =>
                            new EndpointInformation(tessagingRegistry.HandledRemoteTessageTypeIds(), configuration));

      registrar.ForQuery((NetworkTopologyQuery _, IEndpointRegistry endpointRegistry) =>
                            new NetworkTopology(endpointRegistry.ServerEndpointAddresses));
   }
}
