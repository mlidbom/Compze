using Compze.Abstractions.Hosting.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.CollectionsCE.GenericCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Tessaging.Implementation.Transport;
using Compze.Tessaging.Transport.SqlLayer;
using Compze.Threading;
using JetBrains.Annotations;

namespace Compze.Tessaging.Implementation.Peers;

static class PeerRegistryRegistrar
{
   public static IComponentRegistrar PeerRegistry(this IComponentRegistrar registrar)
      => registrar.Register(Singleton.ForSet<IPeerRegistry>()
                                     .CreatedBy((IServiceBusSqlLayer.IPeerRegistrySqlLayer sqlLayer) => new PeerRegistry(sqlLayer)));
}

///<summary>The <see cref="IPeerRegistry"/>: the durable peer tables fronted by an in-memory mirror, so reads never touch the<br/>
/// database and writes hit it once per advertisement fetch.</summary>
[UsedImplicitly] class PeerRegistry : IPeerRegistry
{
   readonly IServiceBusSqlLayer.IPeerRegistrySqlLayer _sqlLayer;
   readonly IMonitor _monitor = IMonitor.New();
   IReadOnlyDictionary<EndpointId, IServiceBusSqlLayer.PersistedPeer> _peers = new Dictionary<EndpointId, IServiceBusSqlLayer.PersistedPeer>();

   internal PeerRegistry(IServiceBusSqlLayer.IPeerRegistrySqlLayer sqlLayer) => _sqlLayer = sqlLayer;

   public async Task StartAsync()
   {
      await _sqlLayer.InitAsync().caf();
      var rememberedPeers = _sqlLayer.GetPeers().ToDictionary(peer => peer.Id);
      _monitor.Locked(() => _peers = rememberedPeers);
   }

   public void RecordAdvertisement(TessagingEndpointInformation advertisement)
   {
      //Its own transaction, never the caller's: recording an advertisement is fact-keeping - the fetch happened - and must not roll back with any unrelated ambient transaction.
      TransactionScopeCe.SuppressAmbient(() => TransactionScopeCe.Execute(() => _sqlLayer.SaveAdvertisement(advertisement.Id, advertisement.HandledTessageTypes)));
      _monitor.Locked(() => _peers = _peers.SetInCopy(advertisement.Id, new IServiceBusSqlLayer.PersistedPeer(advertisement.Id, advertisement.HandledTessageTypes)));
   }

   public IReadOnlyList<IServiceBusSqlLayer.PersistedPeer> Peers => _monitor.Locked(() => (IReadOnlyList<IServiceBusSqlLayer.PersistedPeer>)[.._peers.Values]);
}
