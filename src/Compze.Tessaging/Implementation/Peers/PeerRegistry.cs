using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.CollectionsCE.GenericCE;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Tessaging.Implementation.Transport;
using Compze.Tessaging.Transport.SqlLayer;
using Compze.Threading;
using Compze.TypeIdentifiers;
using JetBrains.Annotations;

namespace Compze.Tessaging.Implementation.Peers;

static class PeerRegistryRegistrar
{
   public static IComponentRegistrar PeerRegistry(this IComponentRegistrar registrar)
      => registrar.Register(Singleton.ForSet<IPeerRegistry>()
                                     .CreatedBy((IServiceBusSqlLayer.IPeerRegistrySqlLayer sqlLayer, ITypeMap typeMap) => new PeerRegistry(sqlLayer, typeMap)));
}

///<summary>The <see cref="IPeerRegistry"/>: the durable peer tables fronted by an in-memory mirror, so reads never touch the<br/>
/// database and writes hit it once per advertisement fetch.</summary>
[UsedImplicitly] class PeerRegistry : IPeerRegistry
{
   readonly IServiceBusSqlLayer.IPeerRegistrySqlLayer _sqlLayer;
   readonly ITypeMap _typeMap;
   readonly IMonitor _monitor = IMonitor.New();
   IReadOnlyDictionary<EndpointId, RememberedPeer> _peers = new Dictionary<EndpointId, RememberedPeer>();

   internal PeerRegistry(IServiceBusSqlLayer.IPeerRegistrySqlLayer sqlLayer, ITypeMap typeMap)
   {
      _sqlLayer = sqlLayer;
      _typeMap = typeMap;
   }

   public async Task StartAsync()
   {
      await _sqlLayer.InitAsync().caf();
      var rememberedPeers = _sqlLayer.GetPeers().ToDictionary(peer => peer.Id, peer => new RememberedPeer(peer, _typeMap));
      _monitor.Locked(() => _peers = rememberedPeers);
   }

   public void RecordAdvertisement(TessagingEndpointInformation advertisement)
   {
      //Its own transaction, never the caller's: recording an advertisement is fact-keeping - the fetch happened - and must not roll back with any unrelated ambient transaction.
      TransactionScopeCe.SuppressAmbient(() => TransactionScopeCe.Execute(() => _sqlLayer.SaveAdvertisement(advertisement.Id, advertisement.HandledTessageTypes)));
      var rememberedPeer = new RememberedPeer(new IServiceBusSqlLayer.PersistedPeer(advertisement.Id, advertisement.HandledTessageTypes), _typeMap);
      _monitor.Locked(() => _peers = _peers.SetInCopy(advertisement.Id, rememberedPeer));
   }

   public IReadOnlyList<IServiceBusSqlLayer.PersistedPeer> Peers => _monitor.Locked(() => (IReadOnlyList<IServiceBusSqlLayer.PersistedPeer>)[.._peers.Values.Select(peer => peer.Persisted)]);

   public IReadOnlyList<EndpointId> SubscriberIdsFor(IPublisherTevent<IRemotableTevent> wrappedTevent) =>
      _monitor.Locked(() => (IReadOnlyList<EndpointId>)[.._peers.Values.Where(peer => peer.SubscribesTo(wrappedTevent)).Select(peer => peer.Persisted.Id)]);

   public IReadOnlyList<EndpointId> HandlerIdsFor(IExactlyOnceTommand tommand) =>
      _monitor.Locked(() => (IReadOnlyList<EndpointId>)[.._peers.Values.Where(peer => peer.Handles(tommand)).Select(peer => peer.Persisted.Id)]);

   ///<summary>One remembered peer with its advertised type strings resolved to types once, when the peer is remembered, so<br/>
   /// <see cref="SubscribesTo"/> and <see cref="Handles"/> are pure type checks on every publish and send. The advertisement<br/>
   /// partitions the way the router's route registration partitions it: tevent subscriptions are wrapper types matched by<br/>
   /// assignability, everything else is a tommand type matched exactly — so the registry and the routes always agree.</summary>
   class RememberedPeer
   {
      internal IServiceBusSqlLayer.PersistedPeer Persisted { get; }
      readonly IReadOnlyList<Type> _teventSubscriptions;
      readonly HashSet<Type> _handledTommandTypes;

      internal RememberedPeer(IServiceBusSqlLayer.PersistedPeer persisted, ITypeMap typeMap)
      {
         Persisted = persisted;
         var advertisedTypes = persisted.HandledTessageTypes.Select(typeIdString => typeMap.GetId(typeIdString).Type).ToList();
         _teventSubscriptions = [..advertisedTypes.Where(advertisedType => advertisedType.Is<ITevent>())];
         _handledTommandTypes = [..advertisedTypes.Where(advertisedType => !advertisedType.Is<ITevent>())];
      }

      ///<summary>Whether this peer's last-known advertisement subscribes to <paramref name="wrappedTevent"/> — the same<br/>
      /// advertised-wrapper-type assignability test the router's routes apply.</summary>
      internal bool SubscribesTo(IPublisherTevent<IRemotableTevent> wrappedTevent)
         => _teventSubscriptions.Any(subscription => subscription.IsInstanceOfType(wrappedTevent));

      ///<summary>Whether this peer's last-known advertisement handles <paramref name="tommand"/>'s type — the same exact-type<br/>
      /// match the router's tommand routes apply.</summary>
      internal bool Handles(IExactlyOnceTommand tommand) => _handledTommandTypes.Contains(tommand.GetType());
   }
}
