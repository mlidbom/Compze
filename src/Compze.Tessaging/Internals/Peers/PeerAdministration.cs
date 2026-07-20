using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Internals.Transport.Client.Internal;

namespace Compze.Tessaging.Internals.Peers;

static class PeerAdministrationRegistrar
{
   ///<summary>Registers the endpoint's one <see cref="IPeerAdministration"/> — the administrative operations on the peer memory<br/>
   /// every transport-speaking endpoint keeps. The delivery tiers that hold tessages for peers each contribute an<br/>
   /// <see cref="IPeerDecommissionParticipant"/> to the set this resolves.</summary>
   public static IComponentRegistrar PeerAdministration(this IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<IPeerAdministration>()
                                     .CreatedBy((IPeerRegistry peerRegistry, ITessagingRouter router, IComponentSet<IPeerDecommissionParticipant> decommissionParticipants, EndpointConfiguration configuration)
                                                   => new PeerAdministration(peerRegistry, router, [..decommissionParticipants], configuration)));
}

///<summary>The <see cref="IPeerAdministration"/>: coordinates a decommission across the registry (membership) and every<br/>
/// delivery tier that keeps tessages for peers (<see cref="IPeerDecommissionParticipant"/>), as one transaction of its own.</summary>
class PeerAdministration : IPeerAdministration
{
   readonly IPeerRegistry _peerRegistry;
   readonly ITessagingRouter _router;
   readonly IReadOnlyList<IPeerDecommissionParticipant> _decommissionParticipants;
   readonly EndpointConfiguration _configuration;

   internal PeerAdministration(IPeerRegistry peerRegistry, ITessagingRouter router, IReadOnlyList<IPeerDecommissionParticipant> decommissionParticipants, EndpointConfiguration configuration)
   {
      _peerRegistry = peerRegistry;
      _router = router;
      _decommissionParticipants = decommissionParticipants;
      _configuration = configuration;
   }

   public async Task<PeerDecommissionReport> DecommissionAsync(EndpointId peer)
   {
      State.Assert(!peer.Equals(_configuration.Id), () => "An endpoint cannot decommission itself: a peer is another endpoint.");
      State.Assert(!_router.HasLiveConnectionTo(peer),
                   () => $"Peer {peer} is currently connected. Decommissioning is the administrative declaration that a peer is gone for good - a connected peer is not gone. Take the peer down first.");

      var peerIsRemembered = _peerRegistry.Peers.Any(it => it.Id.Equals(peer));

      PeerDecommissionReport report = null!;
      //One act, one transaction - its own, never a caller's: the registry's durable removal and the durable tiers' discards
      //commit together, and every in-memory consequence (the registry forgetting the peer, the best-effort queues dropping
      //theirs) runs only on commit - so an act that fails partway, the unknown-peer failure below included, changes nothing.
      await TransactionScopeCe.SuppressAmbientAsync(async () => await TransactionScopeCe.ExecuteAsync(async () =>
      {
         await _peerRegistry.DecommissionAsync(peer).caf();
         List<PeerDecommissionReport.DiscardedTessages> discarded = [];
         foreach(var participant in _decommissionParticipants)
            discarded.AddRange(await participant.DiscardEverythingKeptForAsync(peer).caf());
         State.Assert(peerIsRemembered || discarded.Count > 0,
                      () => $"{peer} is not a peer this endpoint knows: no advertisement of it was ever recorded - or it was already decommissioned - and nothing is held for it.");
         report = new PeerDecommissionReport(peer, discarded);
      }).caf()).caf();

      this.Log().Warning($"Decommissioned peer {peer}."
                       + (report.Discarded.Count == 0
                             ? " Nothing was owed it."
                             : $" Discarded: {string.Join("; ", report.Discarded.Select(it => $"{it.Count} {it.Description}"))}."));
      return report;
   }
}
