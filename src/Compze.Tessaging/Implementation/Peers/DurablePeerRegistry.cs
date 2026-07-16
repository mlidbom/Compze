using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Tessaging.Implementation.Transport;
using Compze.Tessaging.Transport.SqlLayer;
using Compze.TypeIdentifiers;
using JetBrains.Annotations;

namespace Compze.Tessaging.Implementation.Peers;

///<summary>The <see cref="IPeerRegistry"/> of an endpoint whose foundation declares Tessaging persistence: the durable peer<br/>
/// tables fronted by the in-memory <see cref="RememberedPeers"/>, so reads never touch the database and writes hit it once per<br/>
/// advertisement fetch. Peer memory survives restarts on both sides: a peer is remembered until explicitly decommissioned.</summary>
[UsedImplicitly] class DurablePeerRegistry : IPeerRegistry
{
   readonly IServiceBusSqlLayer.IPeerRegistrySqlLayer _sqlLayer;
   readonly ITypeMap _typeMap;
   readonly RememberedPeers _rememberedPeers = new();

   internal DurablePeerRegistry(IServiceBusSqlLayer.IPeerRegistrySqlLayer sqlLayer, ITypeMap typeMap)
   {
      _sqlLayer = sqlLayer;
      _typeMap = typeMap;
   }

   public async Task StartAsync()
   {
      await _sqlLayer.InitAsync().caf();
      _rememberedPeers.ReplaceAllWith(_sqlLayer.GetPeers().Select(peer => new RememberedPeer(peer.Id, peer.HandledTessageTypes, _typeMap)));
   }

   public void RecordAdvertisement(TessagingEndpointInformation advertisement)
   {
      //Its own transaction, never the caller's: recording an advertisement is fact-keeping - the fetch happened - and must not roll back with any unrelated ambient transaction.
      TransactionScopeCe.SuppressAmbient(() => TransactionScopeCe.Execute(() => _sqlLayer.SaveAdvertisement(advertisement.Id, advertisement.HandledTessageTypes)));
      _rememberedPeers.Remember(new RememberedPeer(advertisement.Id, advertisement.HandledTessageTypes, _typeMap));
   }

   public IReadOnlyList<RememberedPeer> Peers => _rememberedPeers.Peers;

   public IReadOnlyList<EndpointId> SubscriberIdsFor(IPublisherTevent<IRemotableTevent> wrappedTevent) => _rememberedPeers.SubscriberIdsFor(wrappedTevent);

   public IReadOnlyList<EndpointId> HandlerIdsFor(IExactlyOnceTommand tommand) => _rememberedPeers.HandlerIdsFor(tommand);
}
