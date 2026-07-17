using System.Transactions;
using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Contracts;
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
   readonly IReadOnlyList<IPeerLifecycleObserver> _lifecycleObservers;
   readonly RememberedPeers _rememberedPeers = new();

   internal DurablePeerRegistry(IServiceBusSqlLayer.IPeerRegistrySqlLayer sqlLayer, ITypeMap typeMap, IReadOnlyList<IPeerLifecycleObserver> lifecycleObservers)
   {
      _sqlLayer = sqlLayer;
      _typeMap = typeMap;
      _lifecycleObservers = lifecycleObservers;
   }

   public async Task StartAsync()
   {
      await _sqlLayer.InitAsync().caf();
      _rememberedPeers.ReplaceAllWith(_sqlLayer.GetPeers().Select(peer => new RememberedPeer(peer.Id, peer.HandledTessageTypes, _typeMap)));
   }

   public void RecordAdvertisement(TessagingEndpointInformation advertisement)
   {
      var peer = new RememberedPeer(advertisement.Id, advertisement.HandledTessageTypes, _typeMap);
      var previous = _rememberedPeers.Find(peer.Id);
      //Its own transaction, never the caller's: recording an advertisement is fact-keeping - the fetch happened - and must not
      //roll back with any unrelated ambient transaction. The lifecycle observers are notified inside it, so the recorded
      //advertisement and its consequences - the outbox pruning what a shrink orphaned - commit or roll back together.
      TransactionScopeCe.SuppressAmbient(() => TransactionScopeCe.Execute(() =>
      {
         _sqlLayer.SaveAdvertisement(advertisement.Id, advertisement.HandledTessageTypes);
         _lifecycleObservers.NotifyAdvertisementRecorded(previous, peer);
      }));
      _rememberedPeers.Remember(peer);
   }

   public void Decommission(EndpointId peer)
   {
      State.NotNull(Transaction.Current);
      _sqlLayer.DeletePeer(peer);
      //The mirror follows only on commit: fan-out and receiver binding must keep seeing the peer while the act can still roll back.
      Transaction.Current.OnCommittedSuccessfully(() => _rememberedPeers.Forget(peer));
   }

   public IReadOnlyList<RememberedPeer> Peers => _rememberedPeers.Peers;

   public IReadOnlyList<EndpointId> SubscriberIdsFor(IPublisherTevent<IRemotableTevent> wrappedTevent) => _rememberedPeers.SubscriberIdsFor(wrappedTevent);

   public IReadOnlyList<EndpointId> HandlerIdsFor(IExactlyOnceTommand tommand) => _rememberedPeers.HandlerIdsFor(tommand);
}
