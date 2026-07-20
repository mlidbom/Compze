using Compze.Tessaging.Endpoints;
using Compze.DependencyInjection;
using Compze.Must;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.Peers;
using Compze.Tests.Common.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;

using static Compze.Must.MustActions;

namespace Compze.Tests.Integration.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

///<summary>Decommissioning is the one way a peer leaves the endpoint's memory — an administrative act, never an inference<br/>
/// (see <c>src/Compze.Tessaging/dev_docs/peer-model.md</c>): <see cref="IPeerAdministration.DecommissionAsync"/> removes the peer —<br/>
/// publishes stop fanning out to it, sends stop binding to it — and discards everything the endpoint still held for it,<br/>
/// reported by the act, never as a silent side effect. A decommissioned peer that later re-announces is a first contact again.</summary>
[LongRunning]
public class Peer_decommission_tests : EndpointHostTestBase
{
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
      BackendPeerAdministration.Peers.Any(peer => peer.Id.Equals(RemoteEndpointId)).Must().BeFalse();

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
      //Strand the tommand through the real conversation (see Advertisement_shrink_tests): the Backend met the Remote endpoint,
      //the Remote endpoint went down, the send bound to it as the sole remembered handler, and it returned no longer handling
      //the type - the tommand is stranded, awaiting exactly this resolution.
      await Host.DisposeAsync();
      await StartHostWithOnlyTheBackendEndpointAsync();
      await BackendEndPoint.ServiceLocator.Resolve<IIndependentTommandSender>().SendAsync(new MyExactlyOnceTommandHandledByTheRemoteEndpoint());
      await Host.DisposeAsyncWithoutWaitingForEndpointsToBeAtRest(); //The tommand is undelivered: awaiting rest would wait for Remote's return.
      await StartHostWithTheRemoteEndpointReturningNoLongerHandlingItsTommandAsync();

      //The returned endpoint does not receive the tommand - the strand is the shrink's doing, and this bounded non-delivery
      //window is also what gives the shrink time to record before the rebuild below.
      MyExactlyOnceTommandHandledByTheRemoteEndpointHandlerThreadGate.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(2)).Must().BeFalse();

      //Decommissioning requires the peer down: rebuild once more without it.
      await Host.DisposeAsync();
      await StartHostWithOnlyTheBackendEndpointAsync();

      var strandedEntry = (await BackendPeerAdministration.DecommissionAsync(RemoteEndpointId)).Discarded.Single();

      strandedEntry.Description.Must().Contain("stranded");
      strandedEntry.Description.Must().Contain(nameof(MyExactlyOnceTommandHandledByTheRemoteEndpoint));
      strandedEntry.Count.Must().Be(1);
   }

   [PCT] public async Task A_renounced_tevent_discarded_at_shrink_is_absent_from_the_peers_decommission_report()
   {
      //Discard the tevent through the real conversation: the Backend met the Remote endpoint, the Remote endpoint went down,
      //a tevent its remembered subscription matches was published in its absence, and it returned having renounced every
      //tevent subscription - the tevent was discarded at the shrink, by the audience's own choice, not kept stranded.
      await Host.DisposeAsync();
      await StartHostWithOnlyTheBackendEndpointAsync();
      await Navigator.PostAsync(MyCreateTaggregateTommand.Create());
      await Host.DisposeAsyncWithoutWaitingForEndpointsToBeAtRest(); //The tevent is undelivered: awaiting rest would wait for Remote's return.
      await StartHostWithTheRemoteEndpointReturningHavingRenouncedItsTeventSubscriptionsAsync();

      //The returned endpoint does not receive the tevent - the discard is the shrink's doing, and this bounded non-delivery
      //window is also what gives the shrink time to record before the rebuild below.
      MyRemoteTaggregateTeventHandlerThreadGate.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(2)).Must().BeFalse();

      //Decommissioning requires the peer down: rebuild once more without it.
      await Host.DisposeAsync();
      await StartHostWithOnlyTheBackendEndpointAsync();

      (await BackendPeerAdministration.DecommissionAsync(RemoteEndpointId)).Discarded.Must().BeEmpty();
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
}
