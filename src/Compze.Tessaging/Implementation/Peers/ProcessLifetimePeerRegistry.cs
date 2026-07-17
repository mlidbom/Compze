using Compze.Tessaging.Internals.Transport;
using System.Transactions;
using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Contracts;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Tessaging.Implementation.Transport;
using Compze.TypeIdentifiers;
using JetBrains.Annotations;

namespace Compze.Tessaging.Implementation.Peers;

///<summary>The <see cref="IPeerRegistry"/> of a database-less endpoint: the peers met during this process's lifetime, remembered<br/>
/// in memory (<see cref="RememberedPeers"/>) and treated the same as a durably remembered peer for as long as the process lives<br/>
/// (see <c>src/Compze.Tessaging/dev_docs/peer-model.md</c>). Nothing survives the process — by design: the composition that persists<br/>
/// nothing has nowhere to keep more.</summary>
[UsedImplicitly] class ProcessLifetimePeerRegistry : IPeerRegistry
{
   readonly ITypeMap _typeMap;
   readonly IReadOnlyList<IPeerLifecycleObserver> _lifecycleObservers;
   readonly RememberedPeers _rememberedPeers = new();

   internal ProcessLifetimePeerRegistry(ITypeMap typeMap, IReadOnlyList<IPeerLifecycleObserver> lifecycleObservers)
   {
      _typeMap = typeMap;
      _lifecycleObservers = lifecycleObservers;
   }

   public Task StartAsync() => Task.CompletedTask;

   public async Task RecordAdvertisementAsync(EndpointInformation advertisement)
   {
      var peer = new RememberedPeer(advertisement.Id, advertisement.HandledTessageTypes, _typeMap);
      var previous = _rememberedPeers.Find(peer.Id);
      _rememberedPeers.Remember(peer);
      await _lifecycleObservers.NotifyAdvertisementRecordedAsync(previous, peer).caf();
   }

   public Task DecommissionAsync(EndpointId peer)
   {
      //Deferred to commit like the durable flavor: the decommission act's other consequences may still fail and roll the act back.
      State.NotNull(Transaction.Current);
      Transaction.Current.OnCommittedSuccessfully(() => _rememberedPeers.Forget(peer));
      return Task.CompletedTask;
   }

   public IReadOnlyList<RememberedPeer> Peers => _rememberedPeers.Peers;

   public IReadOnlyList<EndpointId> SubscriberIdsFor(IPublisherTevent<IRemotableTevent> wrappedTevent) => _rememberedPeers.SubscriberIdsFor(wrappedTevent);

   public IReadOnlyList<EndpointId> HandlerIdsFor(Type tessageType) => _rememberedPeers.HandlerIdsFor(tessageType);
}
