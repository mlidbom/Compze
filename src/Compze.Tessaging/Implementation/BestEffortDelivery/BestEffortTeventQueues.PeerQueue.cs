using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Contracts;
using Compze.Internals.Logging;
using Compze.Tessaging.Implementation.Peers;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Threading;

namespace Compze.Tessaging.Implementation.BestEffortDelivery;

partial class BestEffortTeventQueues
{
   ///<summary>One peer's ordered, in-memory queue of outgoing best-effort tessages. Publishes reserve a slot inside their<br/>
   /// transaction (<see cref="ReserveSlot"/>) and append on commit (<see cref="Enqueue"/>); the peer's live connection's<br/>
   /// best-effort delivery stream dequeues one tessage at a time, keeping delivery in publish order. The queue outlives every<br/>
   /// connection to the peer, which is what lets tessages accumulate while the peer is down and drain when its next connection's<br/>
   /// delivery starts.</summary>
   internal class PeerQueue : IDisposable
   {
      readonly IMonitor _monitor = IMonitor.New();
      readonly Queue<TransportTessage.OutGoing> _queue = new();
      int _reservedSlots;
      bool _awaitingFirstContact;
      bool _drainingStreamAttached;
      readonly AutoResetEvent _enqueued = new(false);

      internal PeerQueue(EndpointId peerId, bool awaitingFirstContact, bool queueingDeclined = false)
      {
         PeerId = peerId;
         _awaitingFirstContact = awaitingFirstContact;
         QueueingDeclined = queueingDeclined;
      }

      internal EndpointId PeerId { get; }

      ///<summary>Whether the composition declared this peer not-queued-for (<c>DoNotQueueTeventsFor</c>): tevents are delivered<br/>
      /// to it only while a connection's stream is draining the queue — published while the peer is down they are dropped, and a<br/>
      /// delivery failure drops its queued stream whole instead of pausing.</summary>
      internal bool QueueingDeclined { get; }

      ///<summary>The peer's connection's best-effort delivery stream declares that it is draining this queue — what makes a<br/>
      /// not-queued-for peer's tevents deliverable at all. Balanced by <see cref="DetachDrainingStream"/> when the stream stops.</summary>
      internal void AttachDrainingStream() => _monitor.Locked(() => _drainingStreamAttached = true);

      internal void DetachDrainingStream() => _monitor.Locked(() => _drainingStreamAttached = false);

      ///<summary>Whether this is a required peer's queue still awaiting its first advertisement: everything published is held<br/>
      /// for it, since which tevents it subscribes to is unknown until first contact resolves the held tevents<br/>
      /// (<see cref="FlushHeldTeventsOnFirstContact"/>).</summary>
      internal bool IsAwaitingFirstContact => _monitor.Locked(() => _awaitingFirstContact);

      ///<summary>Signaled on every <see cref="Enqueue"/> — what the draining stream waits on when the queue is empty.</summary>
      internal WaitHandle EnqueuedSignal => _enqueued;

      ///<summary>First contact with this required peer: its advertisement is finally known, so the tevents held for it resolve —<br/>
      /// the subset its subscriptions match stays queued in publish order for the starting stream to deliver, and the rest,<br/>
      /// held only because the subscriptions were unknown, is discarded. (A tevent whose publish reserved a slot while first<br/>
      /// contact was still awaited may enqueue after this and travel unfiltered; the receiving endpoint dispatches it to its<br/>
      /// zero matching handlers — wasted wire traffic in a tiny window, never wrong behavior.)</summary>
      internal void FlushHeldTeventsOnFirstContact(RememberedPeer peer, ITessagesInFlightTracker tessagesInFlightTracker)
      {
         List<TransportTessage.OutGoing> discarded = [];
         var delivered = 0;
         _monitor.Locked(() =>
         {
            if(!_awaitingFirstContact) return;
            _awaitingFirstContact = false;

            var held = _queue.ToArray();
            _queue.Clear();
            foreach(var heldTessage in held)
            {
               if(peer.SubscribesTo((IPublisherTevent<IRemotableTevent>)heldTessage.Tessage))
               {
                  _queue.Enqueue(heldTessage);
                  delivered++;
               } else
                  discarded.Add(heldTessage);
            }
         });

         if(delivered > 0 || discarded.Count > 0)
            this.Log().Info($"First contact with required peer {PeerId}: {delivered} held tevent(s) match its subscriptions and will deliver in publish order; {discarded.Count} held tevent(s) of types it does not subscribe to are discarded.");
         discarded.ForEach(it => tessagesInFlightTracker.DroppedBeforeDelivery(it, PeerId));
      }

      ///<summary>Reserves the slot the tevent will occupy if its transaction commits — taken at publish, inside the caller's<br/>
      /// transaction, so a full queue fails the publish loud (<see cref="BestEffortTeventQueueOverflowException"/>): backpressure,<br/>
      /// never post-commit shedding. <see cref="Enqueue"/> converts the reservation on commit; a transaction that completes<br/>
      /// without committing releases it (<see cref="ReleaseReservation"/>).</summary>
      internal void ReserveSlot() => _monitor.Locked(() =>
      {
         if(_queue.Count + _reservedSlots >= MaximumQueuedTeventsPerPeer)
            throw new BestEffortTeventQueueOverflowException(PeerId);
         _reservedSlots++;
      });

      ///<summary>Releases a reservation whose transaction completed without committing — or whose publish failed after taking it.</summary>
      internal void ReleaseReservation() => _monitor.Locked(() =>
      {
         State.Assert(_reservedSlots > 0, () => "Every release pairs with a reservation its publish took; releasing with none reserved is a bookkeeping bug.");
         _reservedSlots--;
      });

      ///<summary>Appends <paramref name="transportTessage"/>, converting the reservation its publish took inside the<br/>
      /// now-committed transaction — unless the peer is declared not-queued-for and no stream is draining: then the tessage is<br/>
      /// declined (the reservation still converts) and the caller reports the drop; a not-queued-for peer's tevents exist only<br/>
      /// while it is there to receive them.</summary>
      internal bool Enqueue(TransportTessage.OutGoing transportTessage)
      {
         var enqueued = _monitor.Locked(() =>
         {
            State.Assert(_reservedSlots > 0, () => "Every enqueue converts a reservation its publish took; enqueueing with none reserved is a bookkeeping bug.");
            _reservedSlots--;
            if(QueueingDeclined && !_drainingStreamAttached) return false;
            _queue.Enqueue(transportTessage);
            return true;
         });
         if(enqueued) _enqueued.Set();
         return enqueued;
      }

      ///<summary>Empties the queue, returning what was queued — the drop-stream-whole failure policy of a not-queued-for peer:<br/>
      /// its failed tessage and everything queued behind it are dropped together, so the subscriber's gap is one clean boundary.</summary>
      internal IReadOnlyList<TransportTessage.OutGoing> DequeueAll() => _monitor.Locked(() =>
      {
         IReadOnlyList<TransportTessage.OutGoing> queued = [.._queue];
         _queue.Clear();
         return queued;
      });

      ///<summary>The oldest queued tessage, removed from the queue — or null when the queue is empty. Removed before the send is<br/>
      /// attempted, deliberately: nothing on the best-effort tier is ever re-sent (there is no receiver dedup to make a re-send<br/>
      /// safe), so a tessage whose delivery attempt fails is gone — the one intrinsically ambiguous loss the tier accepts.</summary>
      internal TransportTessage.OutGoing? TryDequeue() => _monitor.Locked(() => _queue.Count > 0 ? _queue.Dequeue() : null);

      public void Dispose() => _enqueued.Dispose();
   }
}
