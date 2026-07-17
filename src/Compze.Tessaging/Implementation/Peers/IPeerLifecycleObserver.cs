namespace Compze.Tessaging.Implementation.Peers;

///<summary>Observes the peer lifecycle events the <see cref="IPeerRegistry"/> records — first contact and advertisement<br/>
/// replacement (see the advertisement lifecycle in <c>dev_docs/TODO/durable-peer-topology.md</c>). Registered as a component<br/>
/// set: each delivery tier that keeps tessages for peers contributes the observer that keeps what it holds consistent with<br/>
/// what the peer's advertisement declares — the outbox contributes one; an endpoint composing no such tier has none.</summary>
///<remarks>The registry notifies observers from inside <see cref="IPeerRegistry.RecordAdvertisement"/> — on the durable<br/>
/// registry, inside the same transaction that persists the advertisement, so a recorded shrink and its consequences commit or<br/>
/// roll back together — and always before the peer's connection starts delivering, so an observer's pruning is complete before<br/>
/// the connection's recovery backlog is loaded.</remarks>
interface IPeerLifecycleObserver
{
   ///<summary>The registry did not know this peer until now — its first advertisement just recorded it. First contact is the<br/>
   /// boundary: nothing can be owed a peer before it is first known, so an observer holding tessages bound to it has found<br/>
   /// leftovers of a decommissioned predecessor identity (a publish racing the decommission) and discards them.</summary>
   void PeerMetForTheFirstTime(RememberedPeer peer);

   ///<summary>The peer's stored advertisement was replaced wholesale by a fresh fetch. A replacement that shrinks is the peer's<br/>
   /// own explicit declaration — an unsubscribe by the subscription's owner — and the observers keep what they hold for the<br/>
   /// peer consistent with it: tessages of renounced types must not be delivered to a peer that no longer serves them.</summary>
   void PeerAdvertisementReplaced(RememberedPeer previous, RememberedPeer current);
}

static class PeerLifecycleObserverCE
{
   extension(IReadOnlyList<IPeerLifecycleObserver> @this)
   {
      ///<summary>Dispatches one recorded advertisement to every observer as the lifecycle event it is: first contact when the<br/>
      /// registry had no <paramref name="previous"/> advertisement for the peer, a wholesale replacement otherwise.</summary>
      internal void NotifyAdvertisementRecorded(RememberedPeer? previous, RememberedPeer current)
      {
         if(previous == null) @this.ForEach(observer => observer.PeerMetForTheFirstTime(current));
         else @this.ForEach(observer => observer.PeerAdvertisementReplaced(previous, current));
      }
   }
}
