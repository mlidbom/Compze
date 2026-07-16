using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Tessaging.Implementation.Transport;
using Compze.TypeIdentifiers;
using JetBrains.Annotations;

namespace Compze.Tessaging.Implementation.Peers;

///<summary>The <see cref="IPeerRegistry"/> of a database-less endpoint: the peers met during this process's lifetime, remembered<br/>
/// in memory (<see cref="RememberedPeers"/>) and treated the same as a durably remembered peer for as long as the process lives<br/>
/// (see <c>dev_docs/TODO/durable-peer-topology.md</c>). Nothing survives the process — by design: the composition that persists<br/>
/// nothing has nowhere to keep more.</summary>
[UsedImplicitly] class ProcessLifetimePeerRegistry : IPeerRegistry
{
   readonly ITypeMap _typeMap;
   readonly RememberedPeers _rememberedPeers = new();

   internal ProcessLifetimePeerRegistry(ITypeMap typeMap) => _typeMap = typeMap;

   public Task StartAsync() => Task.CompletedTask;

   public void RecordAdvertisement(TessagingEndpointInformation advertisement) =>
      _rememberedPeers.Remember(new RememberedPeer(advertisement.Id, advertisement.HandledTessageTypes, _typeMap));

   public IReadOnlyList<RememberedPeer> Peers => _rememberedPeers.Peers;

   public IReadOnlyList<EndpointId> SubscriberIdsFor(IPublisherTevent<IRemotableTevent> wrappedTevent) => _rememberedPeers.SubscriberIdsFor(wrappedTevent);

   public IReadOnlyList<EndpointId> HandlerIdsFor(IExactlyOnceTommand tommand) => _rememberedPeers.HandlerIdsFor(tommand);
}
