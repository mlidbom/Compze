using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Tessaging.Implementation.Transport;
using Compze.Tessaging.Transport.SqlLayer;

namespace Compze.Tessaging.Implementation.Peers;

///<summary>The endpoint's durable memory of its peers — the endpoints it works with: each peer's identity and its last-known<br/>
/// advertisement (which remotable tessage types it handles and subscribes to). Written on every advertisement fetch and loaded<br/>
/// at endpoint start, it survives restarts on both sides: a peer is remembered until explicitly decommissioned — absence<br/>
/// (a crash, a clean stop) is never forgetting (see <c>dev_docs/TODO/durable-peer-topology.md</c>).</summary>
///<remarks>Why it exists: without it, exactly-once tevent fan-out was decided by the live connections at publish time, so a<br/>
/// subscriber that was down when a tevent was published — a routine rolling restart sufficed — silently never received it.<br/>
/// Durable peer memory is what lets membership stop depending on liveness.</remarks>
///<remarks>Registered only by compositions with durable storage (exactly-once Tessaging), as an<br/>
/// <see cref="Compze.DependencyInjection.Abstractions.IComponentSet{TService}"/> contribution: the router records every fetched<br/>
/// advertisement through the set, which is empty on endpoints without durable storage.</remarks>
public interface IPeerRegistry
{
   ///<summary>Records <paramref name="advertisement"/> as the advertising peer's current one, replacing what was stored —<br/>
   /// creating the peer on first contact.</summary>
   void RecordAdvertisement(TessagingEndpointInformation advertisement);

   ///<summary>Every remembered peer, with its last-known advertisement. Served from memory: the registry mirrors its durable<br/>
   /// storage, loaded at start and updated on every <see cref="RecordAdvertisement"/>.</summary>
   IReadOnlyList<IServiceBusSqlLayer.PersistedPeer> Peers { get; }

   ///<summary>The <see cref="EndpointId"/> of every remembered peer whose last-known advertisement subscribes to<br/>
   /// <paramref name="wrappedTevent"/> — exactly-once tevent fan-out's membership: decided by remembered advertisement, never<br/>
   /// by liveness, so a subscribing peer that is down at publish time is still fanned out to and receives the tevent on its<br/>
   /// return.</summary>
   ///<remarks>Subscriptions match by the same advertised-wrapper-type assignability the router's routes apply<br/>
   /// (<see cref="Transport.Client.Internal.ITessagingRouter.SubscriberConnectionsFor"/>), and the router records every<br/>
   /// advertisement before it builds routes from it — so a live subscriber's connection always belongs to a listed peer,<br/>
   /// never the reverse.</remarks>
   IReadOnlyList<EndpointId> SubscriberIdsFor(IPublisherTevent<IRemotableTevent> wrappedTevent);

   ///<summary>The <see cref="EndpointId"/> of every remembered peer whose last-known advertisement handles<br/>
   /// <paramref name="tommand"/>'s type — matched exactly, the way the router's tommand routes match. A tommand binds to its<br/>
   /// one specific receiver at send time, and when no handler is live this list is where the receiver comes from: exactly one<br/>
   /// entry is the known-but-down handler; none means nothing known serves the type; more than one is a handler replacement<br/>
   /// whose retired peer was never decommissioned. The one handler this registry cannot answer for is the endpoint itself —<br/>
   /// a peer is another endpoint — covered by the router's always-live self-connection.</summary>
   IReadOnlyList<EndpointId> HandlerIdsFor(IExactlyOnceTommand tommand);

   ///<summary>Initializes the registry's storage and loads the remembered peers into memory. Runs in the endpoint's listening<br/>
   /// phase, before any endpoint in the host starts sending.</summary>
   Task StartAsync();
}
