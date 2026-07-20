using Compze.Tessaging.Endpoints;

namespace Compze.Tessaging.Internal.Peers;

///<summary>The administrative operations on the endpoint's peer memory — the surface the future bus-management endpoints exist<br/>
/// to expose (see <c>src/Compze.Tessaging/dev_docs/peer-model.md</c>). Every transport-speaking endpoint registers one, alongside its<br/>
/// <see cref="IPeerRegistry"/>.</summary>
public interface IPeerAdministration
{
   ///<summary>Decommissions <paramref name="peer"/>: the one way a peer leaves the endpoint's memory — an administrative act,<br/>
   /// never an inference (absence is not a lifecycle event: a crash, liveness pruning, or a clean stop never touch peer<br/>
   /// memory). The act removes the peer from the <see cref="IPeerRegistry"/> — publishes stop fanning out to it and sends stop<br/>
   /// binding to it — and discards everything the endpoint still held for it: undelivered exactly-once tessages awaiting its<br/>
   /// return, stranded tommands awaiting exactly this resolution, queued best-effort tevents, a required peer's first-contact<br/>
   /// hold. Loud and deliberate: the returned <see cref="PeerDecommissionReport"/> says what was discarded — discarding is part<br/>
   /// of the explicit act, never a side effect — and the act is logged as a warning.</summary>
   ///<remarks>A decommissioned peer that later re-announces is a first contact again: it is remembered anew from its first<br/>
   /// advertisement and receives nothing published before that moment.<br/>
   /// Fails loud when <paramref name="peer"/> is the endpoint itself (a peer is another endpoint), is currently connected<br/>
   /// (decommissioning declares a peer gone for good — a connected peer is not gone; take it down first), or is unknown —<br/>
   /// neither remembered nor held for.<br/>
   /// The whole act is one transaction of its own: durable removals and discards commit together, every in-memory consequence<br/>
   /// runs only on commit, so an act that fails partway changes nothing.</remarks>
   Task<PeerDecommissionReport> DecommissionAsync(EndpointId peer);
}
