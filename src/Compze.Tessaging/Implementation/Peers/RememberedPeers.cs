using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Internals.SystemCE.CollectionsCE.GenericCE;
using Compze.Threading;

namespace Compze.Tessaging.Implementation.Peers;

///<summary>The in-memory peer memory behind every <see cref="IPeerRegistry"/> flavor: a monitor-guarded map of<br/>
/// <see cref="RememberedPeer"/>s that reads never leave. <see cref="DurablePeerRegistry"/> loads it from its storage at start<br/>
/// and updates it alongside every persisted advertisement; <see cref="ProcessLifetimePeerRegistry"/> has nothing else.</summary>
class RememberedPeers
{
   readonly IMonitor _monitor = IMonitor.New();
   IReadOnlyDictionary<EndpointId, RememberedPeer> _peers = new Dictionary<EndpointId, RememberedPeer>();

   internal void Remember(RememberedPeer peer) => _monitor.Locked(() => _peers = _peers.SetInCopy(peer.Id, peer));

   internal void ReplaceAllWith(IEnumerable<RememberedPeer> peers) => _monitor.Locked(() => _peers = peers.ToDictionary(peer => peer.Id));

   internal IReadOnlyList<RememberedPeer> Peers => _monitor.Locked(() => (IReadOnlyList<RememberedPeer>)[.._peers.Values]);

   internal IReadOnlyList<EndpointId> SubscriberIdsFor(IPublisherTevent<IRemotableTevent> wrappedTevent) =>
      _monitor.Locked(() => (IReadOnlyList<EndpointId>)[.._peers.Values.Where(peer => peer.SubscribesTo(wrappedTevent)).Select(peer => peer.Id)]);

   internal IReadOnlyList<EndpointId> HandlerIdsFor(IExactlyOnceTommand tommand) =>
      _monitor.Locked(() => (IReadOnlyList<EndpointId>)[.._peers.Values.Where(peer => peer.Handles(tommand)).Select(peer => peer.Id)]);
}
