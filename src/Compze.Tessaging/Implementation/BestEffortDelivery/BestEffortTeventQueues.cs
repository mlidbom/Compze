using Compze.Abstractions.Hosting.Public;
using Compze.Contracts;
using Compze.Tessaging.Implementation.Peers;
using Compze.Tessaging.Implementation.Transport;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Threading;
using Compze.TypeIdentifiers;

namespace Compze.Tessaging.Implementation.BestEffortDelivery;

///<summary>The best-effort tier's in-memory counterpart of the outbox's storage: one ordered queue of outgoing tessages per<br/>
/// peer (<see cref="PeerQueue"/>), owned here — never by a connection — so the queue survives connection churn. The<br/>
/// <see cref="BestEffortTeventDeliveryLeg"/> enqueues into it on commit whether or not the peer is currently connected, and the<br/>
/// connection's best-effort delivery stream drains the peer's queue while one is live: a known peer that is down accumulates its<br/>
/// tessages in order and receives them on its return (see <c>dev_docs/TODO/durable-peer-topology.md</c>). Being memory, it<br/>
/// promises nothing across a crash of this process — that is the exactly-once tier's job.</summary>
///<remarks>A required peer the endpoint has never met (<c>RequirePeers</c> on the distributed Tessaging feature) has its queue<br/>
/// from the start, awaiting first contact: everything published is held for it — its subscriptions are unknown until its first<br/>
/// advertisement — and the matching subset delivers, in order, when it is first met (<see cref="RecordFirstContact"/>). That<br/>
/// makes startup deterministic: nothing a required peer should see is lost to the discovery race.</remarks>
partial class BestEffortTeventQueues : IDisposable
{
   ///<summary>The bound on each peer's queue — generous: a lot of tevents, little memory on current hardware. A publish that<br/>
   /// would exceed it fails loud (<see cref="BestEffortTeventQueueOverflowException"/>): backpressure loses nothing, while<br/>
   /// silently shedding queued tevents does (see <c>dev_docs/TODO/durable-peer-topology.md</c>).</summary>
   internal const int MaximumQueuedTeventsPerPeer = 10_000;

   readonly ITypeMap _typeMap;
   readonly ITessagesInFlightTracker _tessagesInFlightTracker;
   readonly IMonitor _monitor = IMonitor.New();
   readonly Dictionary<EndpointId, PeerQueue> _queues = new();

   internal BestEffortTeventQueues(IReadOnlyList<EndpointId> requiredPeers, IReadOnlyList<EndpointId> peersNotQueuedFor, ITypeMap typeMap, ITessagesInFlightTracker tessagesInFlightTracker)
   {
      State.Assert(!requiredPeers.Intersect(peersNotQueuedFor).Any(),
                   () => $"A peer cannot be both required and not-queued-for: requiring a peer means holding everything for it until it is met, declining to queue means keeping nothing for it while it is away. Declared as both: {string.Join(", ", requiredPeers.Intersect(peersNotQueuedFor))}.");
      _typeMap = typeMap;
      _tessagesInFlightTracker = tessagesInFlightTracker;
      foreach(var requiredPeer in requiredPeers.Distinct())
         _queues.Add(requiredPeer, new PeerQueue(requiredPeer, awaitingFirstContact: true));
      foreach(var peerNotQueuedFor in peersNotQueuedFor.Distinct())
         _queues.Add(peerNotQueuedFor, new PeerQueue(peerNotQueuedFor, awaitingFirstContact: false, queueingDeclined: true));
   }

   ///<summary>The queue holding what this endpoint owes <paramref name="peer"/> — created on first use and living until the<br/>
   /// endpoint does: the peer's connections come and go around it.</summary>
   internal PeerQueue For(EndpointId peer) => _monitor.Locked(() =>
   {
      if(!_queues.TryGetValue(peer, out var queue))
         _queues.Add(peer, queue = new PeerQueue(peer, awaitingFirstContact: false));
      return queue;
   });

   ///<summary>The required peers whose first advertisement has not arrived yet — the peers everything published is held for.</summary>
   internal IReadOnlyList<EndpointId> PeersAwaitingFirstContact =>
      _monitor.Locked(() => (IReadOnlyList<EndpointId>)[.._queues.Values.Where(queue => queue.IsAwaitingFirstContact).Select(queue => queue.PeerId)]);

   ///<summary>Called when a connection to <paramref name="advertisement"/>'s peer starts delivering. For a required peer met for<br/>
   /// the first time this resolves its held tevents against its just-learned subscriptions: the matching subset stays queued in<br/>
   /// publish order — the draining stream delivers it next — and the rest, held only because the peer's subscriptions were<br/>
   /// unknown, is discarded. For every other peer this is a no-op.</summary>
   internal void RecordFirstContact(TessagingEndpointInformation advertisement)
   {
      var queue = For(advertisement.Id);
      if(!queue.IsAwaitingFirstContact) return;
      queue.FlushHeldTeventsOnFirstContact(new RememberedPeer(advertisement.Id, advertisement.HandledTessageTypes, _typeMap), _tessagesInFlightTracker);
   }

   public void Dispose() => _monitor.Locked(() =>
   {
      foreach(var queue in _queues.Values)
         queue.Dispose();
      _queues.Clear();
   });
}
