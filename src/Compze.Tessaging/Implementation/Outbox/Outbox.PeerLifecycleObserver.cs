using Compze.Abstractions.Tessaging.Public;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Tessaging.Implementation.Peers;
using Compze.Tessaging.Transport.SqlLayer;
using Compze.TypeIdentifiers;

namespace Compze.Tessaging.Implementation.Outbox;

// ReSharper disable once ClassCannotBeInstantiated rider is plain confused
partial class Outbox
{
   ///<summary>The outbox's side of the peer lifecycle: keeps what the outbox owes a peer consistent with what the peer's own<br/>
   /// advertisement declares (see the advertisement lifecycle in <c>dev_docs/TODO/durable-peer-topology.md</c>). On every<br/>
   /// advertisement replacement it reconciles the peer's undelivered tessages against the fresh advertisement — a shrunk one is<br/>
   /// the peer's explicit renunciation, so undelivered tevents no remaining subscription matches are discarded, loudly, and<br/>
   /// undelivered tommands of types the peer no longer handles are stranded, loudly: delivering either would hand the peer<br/>
   /// tessages it just declared it does not serve. On first contact it discards anything already bound to the peer — nothing<br/>
   /// can be owed a peer before it is first known, so such rows are leftovers of a decommissioned predecessor identity.</summary>
   ///<remarks>Notified inside the transaction that persists the advertisement and always before the peer's connection loads its<br/>
   /// recovery backlog, so what is pruned here never enters a delivery stream. Reconciliation runs on every replacement, not<br/>
   /// only detected shrinks, deliberately: a publish fanning out on the not-yet-replaced peer memory can commit a row of a<br/>
   /// renounced type concurrently with the shrink that renounced it, and the rerun on the peer's next advertisement prunes it.</remarks>
   ///<remarks>A stranded tommand stays stranded even when a later advertisement re-grows the type: while it was stranded, later<br/>
   /// tessages in the pair's stream kept delivering, so quietly re-scheduling it would deliver it out of order. Resolution is<br/>
   /// explicit, on the decommission surface.</remarks>
   internal class PeerLifecycleObserver : IPeerLifecycleObserver
   {
      readonly ITessageStorage _storage;

      internal PeerLifecycleObserver(ITessageStorage storage) => _storage = storage;

      public void PeerMetForTheFirstTime(RememberedPeer peer)
      {
         var leftovers = _storage.DiscardAllTessagesOwedTo(peer.Id);
         if(leftovers.Count == 0) return;
         this.Log().Warning($"First contact with peer {peer.Id}: discarded {leftovers.Count} tessage(s) already bound to its identity - leftovers of a decommissioned predecessor, since nothing can be owed a peer before it is first known. Types: {DistinctTypeNames(leftovers.Select(it => it.TypeId))}.");
      }

      public void PeerAdvertisementReplaced(RememberedPeer previous, RememberedPeer current)
      {
         var undelivered = _storage.GetUndeliveredTessagesForEndpoint(current.Id);
         if(undelivered.Count == 0) return;

         List<IServiceBusSqlLayer.UndeliveredTessage> renouncedTevents = [], noLongerHandledTommands = [];
         foreach(var tessage in undelivered)
         {
            var tessageType = tessage.TypeId.Type;
            if(tessageType.Is<ITevent>())
            {
               if(!current.SubscribesToTeventsOf(tessageType)) renouncedTevents.Add(tessage);
            } else if(!current.HandlesTommandsOf(tessageType))
            {
               noLongerHandledTommands.Add(tessage);
            }
         }

         DiscardTheRenouncedTevents();
         StrandTheNoLongerHandledTommands();
         return;

         void DiscardTheRenouncedTevents()
         {
            if(renouncedTevents.Count == 0) return;
            _storage.DiscardUndeliveredTessages(current.Id, [..renouncedTevents.Select(it => it.TessageId)]);
            this.Log().Warning($"Peer {current.Id}'s replaced advertisement no longer subscribes to the type(s) of {renouncedTevents.Count} undelivered tevent(s) bound to it. They lost their audience by that audience's own choice and are discarded. Types: {DistinctTypeNames(renouncedTevents.Select(it => it.TypeId))}.");
         }

         void StrandTheNoLongerHandledTommands()
         {
            if(noLongerHandledTommands.Count == 0) return;
            _storage.StrandUndeliveredTessages(current.Id, [..noLongerHandledTommands.Select(it => it.TessageId)]);
            this.Log().Warning($"Peer {current.Id}'s replaced advertisement no longer handles the type(s) of {noLongerHandledTommands.Count} undelivered tommand(s) bound to it - someone commanded an action that now has no handler there, almost certainly a deployment error. They are stranded: kept, but not delivered, until resolved explicitly when the peer is decommissioned. Types: {DistinctTypeNames(noLongerHandledTommands.Select(it => it.TypeId))}.");
         }
      }

      static string DistinctTypeNames(IEnumerable<TypeId> typeIds) => string.Join(", ", typeIds.Select(it => it.Type.FullName).Distinct());
   }
}
