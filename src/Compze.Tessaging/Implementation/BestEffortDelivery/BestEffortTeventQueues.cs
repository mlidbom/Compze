using Compze.Tessaging.Internals.Transport;
using System.Transactions;
using Compze.Tessaging.Endpoints;
using Compze.Contracts;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.Peers;
using Compze.Threading;
using Compze.TypeIdentifiers;

namespace Compze.Tessaging.Implementation.BestEffortDelivery;

///<summary>The best-effort tier's in-memory counterpart of the outbox's storage: one ordered queue of outgoing tessages per<br/>
/// peer (<see cref="PeerQueue"/>), owned here — never by a connection — so the queue survives connection churn. The<br/>
/// <see cref="BestEffortTeventDeliveryLeg"/> enqueues into it on commit whether or not the peer is currently connected, and the<br/>
/// connection's best-effort delivery stream drains the peer's queue while one is live: a known peer that is down accumulates its<br/>
/// tessages in order and receives them on its return (see <c>src/Compze.Tessaging/dev_docs/peer-model.md</c>). Being memory, it<br/>
/// promises nothing across a crash of this process — that is the exactly-once tier's job.</summary>
///<remarks>A required peer the endpoint has never met (<c>RequirePeers</c> on the distributed Tessaging feature) has its queue<br/>
/// from the start, awaiting first contact: everything published is held for it — its subscriptions are unknown until its first<br/>
/// advertisement — and the matching subset delivers, in order, when it is first met (<see cref="ForConnectedPeer"/>). That<br/>
/// makes startup deterministic: nothing a required peer should see is lost to the discovery race.</remarks>
partial class BestEffortTeventQueues : IDisposable, IPeerDecommissionParticipant
{
   ///<summary>The bound on each peer's queue — generous: a lot of tevents, little memory on current hardware. A publish that<br/>
   /// would exceed it fails loud (<see cref="BestEffortTeventQueueOverflowException"/>): backpressure loses nothing, while<br/>
   /// silently shedding queued tevents does (see <c>src/Compze.Tessaging/dev_docs/peer-model.md</c>).</summary>
   internal const int MaximumQueuedTeventsPerPeer = 10_000;

   readonly ITypeMap _typeMap;
   readonly ITessagesInFlightTracker _tessagesInFlightTracker;
   readonly IMonitor _monitor = IMonitor.New();
   readonly Dictionary<EndpointId, PeerQueue> _queues = new();
   readonly HashSet<EndpointId> _peersNotQueuedFor;

   internal BestEffortTeventQueues(IReadOnlyList<EndpointId> requiredPeers, IReadOnlyList<EndpointId> peersNotQueuedFor, ITypeMap typeMap, ITessagesInFlightTracker tessagesInFlightTracker)
   {
      State.Assert(!requiredPeers.Intersect(peersNotQueuedFor).Any(),
                   () => $"A peer cannot be both required and not-queued-for: requiring a peer means holding everything for it until it is met, declining to queue means keeping nothing for it while it is away. Declared as both: {string.Join(", ", requiredPeers.Intersect(peersNotQueuedFor))}.");
      _typeMap = typeMap;
      _tessagesInFlightTracker = tessagesInFlightTracker;
      _peersNotQueuedFor = [..peersNotQueuedFor];
      foreach(var requiredPeer in requiredPeers.Distinct())
         _queues.Add(requiredPeer, new PeerQueue(requiredPeer, awaitingFirstContact: true));
      foreach(var peerNotQueuedFor in _peersNotQueuedFor)
         _queues.Add(peerNotQueuedFor, new PeerQueue(peerNotQueuedFor, awaitingFirstContact: false, queueingDeclined: true));
   }

   ///<summary>The queue holding what this endpoint owes <paramref name="peer"/> — created on first use and living until the<br/>
   /// endpoint does: the peer's connections come and go around it. A decommissioned peer's tombstone is returned as-is,<br/>
   /// declining everything — only the peer's next connection revives it (<see cref="ForConnectedPeer"/>).</summary>
   internal PeerQueue For(EndpointId peer) => _monitor.Locked(() =>
   {
      if(!_queues.TryGetValue(peer, out var queue))
         _queues.Add(peer, queue = new PeerQueue(peer, awaitingFirstContact: false, queueingDeclined: _peersNotQueuedFor.Contains(peer)));
      return queue;
   });

   ///<summary>The required peers whose first advertisement has not arrived yet — the peers everything published is held for.</summary>
   internal IReadOnlyList<EndpointId> PeersAwaitingFirstContact =>
      _monitor.Locked(() => (IReadOnlyList<EndpointId>)[.._queues.Values.Where(queue => queue.IsAwaitingFirstContact).Select(queue => queue.PeerId)]);

   ///<summary>The queue the connected peer's delivery stream drains, resolved when the stream starts. For a required peer met<br/>
   /// for the first time this resolves its held tevents against its just-learned subscriptions: the matching subset stays<br/>
   /// queued in publish order — the starting stream delivers it next — and the rest, held only because the peer's subscriptions<br/>
   /// were unknown, is discarded. For a decommissioned peer's tombstone this is the revival: a re-announce is first contact<br/>
   /// again, so a fresh queue replaces the tombstone (a not-queued-for declaration is the composition's and survives the<br/>
   /// revival). For every other peer: its standing queue.</summary>
   internal PeerQueue ForConnectedPeer(EndpointInformation advertisement)
   {
      PeerQueue? tombstoneToDispose = null;
      var queue = _monitor.Locked(() =>
      {
         if(!_queues.TryGetValue(advertisement.Id, out var existing))
            return _queues[advertisement.Id] = new PeerQueue(advertisement.Id, awaitingFirstContact: false, queueingDeclined: _peersNotQueuedFor.Contains(advertisement.Id));
         if(!existing.IsDecommissioned) return existing;
         tombstoneToDispose = existing;
         return _queues[advertisement.Id] = new PeerQueue(advertisement.Id, awaitingFirstContact: false, queueingDeclined: _peersNotQueuedFor.Contains(advertisement.Id));
      });
      tombstoneToDispose?.Dispose();

      if(queue.IsAwaitingFirstContact)
         queue.FlushHeldTeventsOnFirstContact(new RememberedPeer(advertisement.Id, advertisement.HandledTessageTypes, _typeMap), _tessagesInFlightTracker);
      return queue;
   }

   ///<summary>The best-effort tier's share of decommissioning a peer: what its queue holds — tevents queued awaiting the<br/>
   /// peer's return, or held awaiting a required peer's first contact — is reported, and dropped when the act commits, the<br/>
   /// queue becoming a tombstone (<see cref="PeerQueue.IsDecommissioned"/>). A tier that keeps nothing for the peer reports<br/>
   /// nothing; an empty hold still reports its zero-count ending — the hold itself was something the endpoint kept.</summary>
   public Task<IReadOnlyList<PeerDecommissionReport.DiscardedTessages>> DiscardEverythingKeptForAsync(EndpointId peer)
   {
      var queue = _monitor.Locked(() => _queues.GetValueOrDefault(peer));
      if(queue == null || queue.IsDecommissioned) return Task.FromResult<IReadOnlyList<PeerDecommissionReport.DiscardedTessages>>([]);

      //In-memory, so the drop is deferred to the act's commit: an act that fails partway must change nothing.
      State.NotNull(Transaction.Current);
      Transaction.Current.OnCommittedSuccessfully(() =>
      {
         var dropped = queue.Decommission();
         foreach(var droppedTessage in dropped)
            _tessagesInFlightTracker.DroppedBeforeDelivery(droppedTessage, peer);
      });

      return Task.FromResult<IReadOnlyList<PeerDecommissionReport.DiscardedTessages>>(
         [new PeerDecommissionReport.DiscardedTessages(queue.IsAwaitingFirstContact
                                                          ? "best-effort tevent(s) held awaiting the required peer's first contact - the hold ends with the decommission"
                                                          : "best-effort tevent(s) queued awaiting the peer's return",
                                                       queue.QueuedCount)]);
   }

   public void Dispose() => _monitor.Locked(() =>
   {
      foreach(var queue in _queues.Values)
         queue.Dispose();
      _queues.Clear();
   });
}
