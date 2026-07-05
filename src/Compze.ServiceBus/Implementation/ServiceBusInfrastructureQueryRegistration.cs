using Compze.Abstractions.Hosting.Public;
using Compze.Internals.Transport;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;

namespace Compze.ServiceBus.Implementation;

///<summary>
/// Registers the service bus's endpoint-discovery infrastructure query handlers, so a remote endpoint can ask
/// this one what it is and who it talks to: <see cref="EndpointInformationQuery"/> (the remote tessage types it
/// handles, plus its configuration) and <see cref="NetworkTopologyQuery"/> (the addresses of the endpoints it
/// routes to).
///</summary>
static class ServiceBusInfrastructureQueryRegistration
{
   internal static void RegisterQueryHandlers(InfrastructureQueryRegistrarWithDependencyInjectionSupport registrar)
   {
      registrar.ForQuery((EndpointInformationQuery _, ITessageHandlerRegistry tessagingRegistry, EndpointConfiguration configuration) =>
                            new EndpointInformation(tessagingRegistry.HandledRemoteTessageTypeIds(), configuration));

      registrar.ForQuery((NetworkTopologyQuery _, IEndpointRegistry endpointRegistry) =>
                            new NetworkTopology(endpointRegistry.ServerEndpointAddresses));
   }
}
