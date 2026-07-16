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
      readonly AutoResetEvent _enqueued = new(false);

      internal PeerQueue(EndpointId peerId, bool awaitingFirstContact)
      {
         PeerId = peerId;
         _awaitingFirstContact = awaitingFirstContact;
      }

      internal EndpointId PeerId { get; }

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

      ///<summary>Appends <paramref name="transportTessage"/>, converting the reservation its publish took inside the now-committed transaction.</summary>
      internal void Enqueue(TransportTessage.OutGoing transportTessage)
      {
         _monitor.Locked(() =>
         {
            State.Assert(_reservedSlots > 0, () => "Every enqueue converts a reservation its publish took; enqueueing with none reserved is a bookkeeping bug.");
            _reservedSlots--;
            _queue.Enqueue(transportTessage);
         });
         _enqueued.Set();
      }

      ///<summary>The oldest queued tessage, removed from the queue — or null when the queue is empty. Removed before the send is<br/>
      /// attempted, deliberately: nothing on the best-effort tier is ever re-sent (there is no receiver dedup to make a re-send<br/>
      /// safe), so a tessage whose delivery attempt fails is gone — the one intrinsically ambiguous loss the tier accepts.</summary>
      internal TransportTessage.OutGoing? TryDequeue() => _monitor.Locked(() => _queue.Count > 0 ? _queue.Dequeue() : null);

      public void Dispose() => _enqueued.Dispose();
   }
}
