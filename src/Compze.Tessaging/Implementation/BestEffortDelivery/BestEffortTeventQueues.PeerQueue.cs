using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Threading.ResourceAccess;

namespace Compze.Tessaging.Implementation.BestEffortDelivery;

partial class BestEffortTeventQueues
{
   ///<summary>One peer's ordered, in-memory queue of outgoing best-effort tessages. Publishes append on commit; the peer's live<br/>
   /// connection's best-effort delivery stream dequeues one tessage at a time, keeping delivery in publish order. The queue<br/>
   /// outlives every connection to the peer, which is what lets tessages accumulate while the peer is down and drain when its<br/>
   /// next connection's delivery starts.</summary>
   internal class PeerQueue : IDisposable
   {
      readonly IThreadShared<Queue<TransportTessage.OutGoing>> _queue = IThreadShared.New(new Queue<TransportTessage.OutGoing>());
      readonly AutoResetEvent _enqueued = new(false);

      ///<summary>Signaled on every <see cref="Enqueue"/> — what the draining stream waits on when the queue is empty.</summary>
      internal WaitHandle EnqueuedSignal => _enqueued;

      internal void Enqueue(TransportTessage.OutGoing transportTessage)
      {
         _queue.Locked(queue => queue.Enqueue(transportTessage));
         _enqueued.Set();
      }

      ///<summary>The oldest queued tessage, removed from the queue — or null when the queue is empty. Removed before the send is<br/>
      /// attempted, deliberately: nothing on the best-effort tier is ever re-sent (there is no receiver dedup to make a re-send<br/>
      /// safe), so a tessage whose delivery attempt fails is gone — the one intrinsically ambiguous loss the tier accepts.</summary>
      internal TransportTessage.OutGoing? TryDequeue() => _queue.Locked(queue => queue.Count > 0 ? queue.Dequeue() : null);

      public void Dispose() => _enqueued.Dispose();
   }
}
