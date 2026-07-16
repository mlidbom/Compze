using Compze.DependencyInjection;
using Compze.Must;
using Compze.Tessaging.Implementation.Peers;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Transport.SqlLayer;
using Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Tests.Infrastructure.XUnit;

namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

///<summary>The peer registry (see <c>dev_docs/TODO/durable-peer-topology.md</c>): an exactly-once endpoint durably remembers the<br/>
/// peers it has met — each peer's identity and last-known advertisement, recorded on every advertisement fetch, mirrored in<br/>
/// memory, and persisted in the endpoint's database.</summary>
public class Peer_registry_tests : EndpointHostTestBase
{
   [PCT] public void After_start_the_backend_remembers_the_remote_endpoint_as_a_peer_whose_advertisement_matches_what_the_remote_advertises()
   {
      var rememberedPeer = BackendEndPoint.ServiceLocator.Resolve<IPeerRegistry>().Peers
                                          .Single(peer => peer.Id.Equals(RemoteEndpointId));

      rememberedPeer.HandledTessageTypes.SetEquals(RemoteEndpointAdvertisedTypes).Must().BeTrue();
   }

   [PCT] public void The_remembered_peer_is_persisted_in_the_backends_database_not_only_mirrored_in_memory()
   {
      var persistedPeer = BackendEndPoint.ServiceLocator.Resolve<IServiceBusSqlLayer.IPeerRegistrySqlLayer>().GetPeers()
                                         .Single(peer => peer.Id.Equals(RemoteEndpointId));

      persistedPeer.HandledTessageTypes.SetEquals(RemoteEndpointAdvertisedTypes).Must().BeTrue();
   }

   HashSet<string> RemoteEndpointAdvertisedTypes =>
      [..RemoteEndpoint.ServiceLocator.Resolve<ITessageHandlerRegistry>().HandledRemoteTessageTypeIds().Select(typeId => typeId.CanonicalString)];
}
