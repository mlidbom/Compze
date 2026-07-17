using Compze.Abstractions.Hosting.Public;

namespace Compze.Tessaging.Implementation.Peers;

///<summary>One delivery tier's share of decommissioning a peer (see <see cref="IPeerAdministration.Decommission"/>, which runs<br/>
/// every participant inside the act's transaction). Registered as a component set: each tier that keeps tessages for peers<br/>
/// contributes one — the outbox for undelivered exactly-once tessages, the best-effort delivery wiring for its per-peer queues —<br/>
/// so the administration surface knows no tier by name.</summary>
interface IPeerDecommissionParticipant
{
   ///<summary>Discards everything this tier keeps for <paramref name="peer"/>, returning the report entries describing it —<br/>
   /// empty when the tier keeps nothing. Durable discards ride the act's ambient transaction; in-memory discards are deferred<br/>
   /// to its commit — so a decommission that fails partway discards nothing anywhere.</summary>
   IReadOnlyList<PeerDecommissionReport.DiscardedTessages> DiscardEverythingKeptFor(EndpointId peer);
}
