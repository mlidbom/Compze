using Compze.Tessaging.Internals.Transport;
using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Runtime.Resolution;
using Compze.Must;
using Compze.Tessaging.Implementation.Peers;
using Compze.Tessaging.Implementation.Transport;
using Compze.Tests.Common.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;
using Compze.TypeIdentifiers;

using static Compze.Must.MustActions;

namespace Compze.Tests.Integration.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

///<summary>Decommissioning is the one way a peer leaves the endpoint's memory — an administrative act, never an inference<br/>
/// (see <c>src/Compze.Tessaging/dev_docs/peer-model.md</c>): <see cref="IPeerAdministration.DecommissionAsync"/> removes the peer —<br/>
/// publishes stop fanning out to it, sends stop binding to it — and discards everything the endpoint still held for it,<br/>
/// reported by the act, never as a silent side effect. A decommissioned peer that later re-announces is a first contact again.</summary>
[LongRunning]
public class Peer_decommission_tests : EndpointHostTestBase
{
   ///<summary>A remembered peer this specification plays the router for — see <see cref="Advertisement_shrink_tests"/>, whose<br/>
   /// never-connected-peer technique the stranded-tommand specifications here build on.</summary>
   static readonly EndpointId NeverConnectedPeerId = new(Guid.Parse("0B94D1E6-7A3F-4C52-8E19-D40C6B27F8A3"));

   [PCT] public async Task Decommissioning_a_down_remembered_subscriber_discards_what_it_was_owed_reports_it_and_its_later_return_is_a_first_contact()
   {
      //The Backend met the Remote endpoint when the host started; rebuild the host without it: the peer is down, not gone.
      await Host.DisposeAsync();
      await StartHostWithOnlyTheBackendEndpointAsync();

      //Owed to the down peer: two tevents its remembered subscriptions match, and a tommand bound to it at send.
      await Navigator.PostAsync(MyCreateTaggregateTommand.Create());
      await Navigator.PostAsync(MyCreateTaggregateTommand.Create());
      await BackendEndPoint.ServiceLocator.Resolve<IIndependentTommandSender>().SendAsync(new MyExactlyOnceTommandHandledByTheRemoteEndpoint());

      var report = await BackendPeerAdministration.DecommissionAsync(RemoteEndpointId);

      //The act reports what it discarded...
      report.DecommissionedPeer.Must().Be(RemoteEndpointId);
      report.Discarded.Single().Count.Must().Be(3);
      //...and the peer left the endpoint's memory.
      BackendPeerRegistry.Peers.Any(peer => peer.Id.Equals(RemoteEndpointId)).Must().BeFalse();

      //Published while decommissioned: fanned out to nobody - nothing anywhere remembers the peer.
      await Navigator.PostAsync(MyCreateTaggregateTommand.Create());

      //The returned peer is a first contact: it receives nothing published before its first discovery...
      await Host.DisposeAsync(); //Everything is at rest: nothing is owed the decommissioned peer.
      await StartHostAsync();
      MyRemoteTaggregateTeventHandlerThreadGate.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(2)).Must().BeFalse();
      MyExactlyOnceTommandHandledByTheRemoteEndpointHandlerThreadGate.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(2)).Must().BeFalse();

      //...while a tevent published after its re-announce reaches it: the peer is remembered anew from its first advertisement.
      await Navigator.PostAsync(MyCreateTaggregateTommand.Create());
      MyRemoteTaggregateTeventHandlerThreadGate.AwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(15));
   }

   [PCT] public async Task Decommissioning_the_retired_predecessor_resolves_the_multiple_remembered_handlers_send_failure()
   {
      //Meeting the Remote endpoint and later its successor leaves TWO remembered peers advertising the tommand's type...
      await Host.DisposeAsync();
      await StartHostWithTheBackendEndpointAndASuccessorToTheRemoteEndpointAsync();
      //...and with neither of them live a send cannot know which is current: it fails loud rather than strand the tommand.
      await Host.DisposeAsync();
      await StartHostWithOnlyTheBackendEndpointAsync();
      (await InvokingAsync(async () => await BackendEndPoint.ServiceLocator.Resolve<IIndependentTommandSender>().SendAsync(new MyExactlyOnceTommandHandledByTheRemoteEndpoint()))
         .Must().ThrowAsync<Exception>()).Which.Message.Must().Contain("More than one remembered peer");

      //Decommissioning the retired predecessor resolves the ambiguity - it was owed nothing, and the report says so...
      (await BackendPeerAdministration.DecommissionAsync(RemoteEndpointId)).Discarded.Must().BeEmpty();

      //...the send binds to the sole remaining remembered handler, and the successor receives it on its return.
      await BackendEndPoint.ServiceLocator.Resolve<IIndependentTommandSender>().SendAsync(new MyExactlyOnceTommandHandledByTheRemoteEndpoint());
      await Host.DisposeAsyncWithoutWaitingForEndpointsToBeAtRest(); //The tommand is undelivered: awaiting rest would wait for the successor's return.
      await StartHostWithTheBackendEndpointAndASuccessorToTheRemoteEndpointAsync();
      RemoteSuccessorTommandHandlerThreadGate.AwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(15));
   }

   [PCT] public async Task A_stranded_tommand_is_discarded_and_reported_when_its_peer_is_decommissioned()
   {
      //A tommand is bound to the never-connected peer - the sole remembered handler of its type - and the peer's replaced
      //advertisement then renounces the type: the tommand is stranded, awaiting exactly this resolution (see Advertisement_shrink_tests).
      var tommandTypeIdString = BackendEndPoint.ServiceLocator.Resolve<ITypeMap>().GetId(typeof(MyUnhandledExactlyOnceTommand)).CanonicalString;
      await BackendPeerRegistry.RecordAdvertisementAsync(new EndpointInformation("NeverConnectedPeer", NeverConnectedPeerId, [tommandTypeIdString]));
      await BackendEndPoint.ServiceLocator.Resolve<IIndependentTommandSender>().SendAsync(new MyUnhandledExactlyOnceTommand());
      await BackendPeerRegistry.RecordAdvertisementAsync(new EndpointInformation("NeverConnectedPeer", NeverConnectedPeerId, []));

      var strandedEntry = (await BackendPeerAdministration.DecommissionAsync(NeverConnectedPeerId)).Discarded.Single();

      strandedEntry.Description.Must().Contain("stranded");
      strandedEntry.Description.Must().Contain(nameof(MyUnhandledExactlyOnceTommand));
      strandedEntry.Count.Must().Be(1);
   }

   [PCT] public async Task A_renounced_tevent_discarded_at_shrink_is_absent_from_the_peers_decommission_report()
   {
      //A tevent is owed to the never-connected peer, whose replaced advertisement then renounces every subscription: the
      //tevent was discarded at the shrink - by the audience's own choice - not kept stranded, so the decommission finds nothing.
      var remoteAdvertisement = BackendPeerRegistry.Peers.Single(peer => peer.Id.Equals(RemoteEndpointId)).HandledTessageTypes;
      await BackendPeerRegistry.RecordAdvertisementAsync(new EndpointInformation("NeverConnectedPeer", NeverConnectedPeerId, [..remoteAdvertisement]));
      await Navigator.PostAsync(MyCreateTaggregateTommand.Create());
      await BackendPeerRegistry.RecordAdvertisementAsync(new EndpointInformation("NeverConnectedPeer", NeverConnectedPeerId, []));

      (await BackendPeerAdministration.DecommissionAsync(NeverConnectedPeerId)).Discarded.Must().BeEmpty();
   }

   [PCT] public async Task Decommissioning_a_connected_peer_fails_loud()
   {
      //A tevent delivered to the Remote endpoint proves the connection to it is live.
      await Navigator.PostAsync(MyCreateTaggregateTommand.Create());
      MyRemoteTaggregateTeventHandlerThreadGate.AwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(15));

      (await InvokingAsync(async () => await BackendPeerAdministration.DecommissionAsync(RemoteEndpointId)).Must().ThrowAsync<Exception>())
         .Which.Message.Must().Contain("currently connected");
   }

   [PCT] public async Task Decommissioning_the_endpoint_itself_fails_loud() =>
      (await InvokingAsync(async () => await BackendPeerAdministration.DecommissionAsync(BackendEndpointId)).Must().ThrowAsync<Exception>())
         .Which.Message.Must().Contain("cannot decommission itself");

   [PCT] public async Task Decommissioning_an_unknown_peer_fails_loud() =>
      (await InvokingAsync(async () => await BackendPeerAdministration.DecommissionAsync(new EndpointId(Guid.Parse("8C2D1F5A-0B9E-4D67-A3C4-F51E2B08D9A7")))).Must().ThrowAsync<Exception>())
         .Which.Message.Must().Contain("not a peer this endpoint knows");

   IPeerAdministration BackendPeerAdministration => BackendEndPoint.ServiceLocator.Resolve<IPeerAdministration>();
   IPeerRegistry BackendPeerRegistry => BackendEndPoint.ServiceLocator.Resolve<IPeerRegistry>();
}
