using Compze.Abstractions.Hosting.Public;
using Compze.Threading;

namespace Compze.Tessaging.Implementation.BestEffortDelivery;

///<summary>The best-effort tier's in-memory counterpart of the outbox's storage: one ordered queue of outgoing tessages per<br/>
/// peer (<see cref="PeerQueue"/>), owned here — never by a connection — so the queue survives connection churn. The<br/>
/// <see cref="BestEffortTeventDeliveryLeg"/> enqueues into it on commit whether or not the peer is currently connected, and the<br/>
/// connection's best-effort delivery stream drains the peer's queue while one is live: a known peer that is down accumulates its<br/>
/// tessages in order and receives them on its return (see <c>dev_docs/TODO/durable-peer-topology.md</c>). Being memory, it<br/>
/// promises nothing across a crash of this process — that is the exactly-once tier's job.</summary>
partial class BestEffortTeventQueues : IDisposable
{
   ///<summary>The bound on each peer's queue — generous: a lot of tevents, little memory on current hardware. A publish that<br/>
   /// would exceed it fails loud (<see cref="BestEffortTeventQueueOverflowException"/>): backpressure loses nothing, while<br/>
   /// silently shedding queued tevents does (see <c>dev_docs/TODO/durable-peer-topology.md</c>).</summary>
   internal const int MaximumQueuedTeventsPerPeer = 10_000;

   readonly IMonitor _monitor = IMonitor.New();
   readonly Dictionary<EndpointId, PeerQueue> _queues = new();

   ///<summary>The queue holding what this endpoint owes <paramref name="peer"/> — created on first use and living until the<br/>
   /// endpoint does: the peer's connections come and go around it.</summary>
   internal PeerQueue For(EndpointId peer) => _monitor.Locked(() =>
   {
      if(!_queues.TryGetValue(peer, out var queue))
         _queues.Add(peer, queue = new PeerQueue(peer));
      return queue;
   });

   public void Dispose() => _monitor.Locked(() =>
   {
      foreach(var queue in _queues.Values)
         queue.Dispose();
      _queues.Clear();
   });
}
