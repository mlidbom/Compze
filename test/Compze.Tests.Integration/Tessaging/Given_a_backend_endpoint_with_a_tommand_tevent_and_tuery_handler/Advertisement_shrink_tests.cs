using Compze.Tessaging.Endpoints;
using Compze.DependencyInjection;
using Compze.Must;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.Internal.Peers;
using Compze.Tessaging.Transport.Discovery;
using Compze.Tessaging.Transport.SqlLayer;
using Compze.Tests.Common.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;
using Compze.TypeIdentifiers;

namespace Compze.Tests.Integration.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

///<summary>A shrunk advertisement is the peer's own explicit declaration — an unsubscribe by the subscription's owner — and<br/>
/// what the outbox owes the peer follows it (see the advertisement lifecycle in <c>src/Compze.Tessaging/dev_docs/peer-model.md</c>):<br/>
/// undelivered tevents whose subscriptions the peer renounced are discarded, loudly, and undelivered tommands of types the peer<br/>
/// no longer handles are stranded, loudly — kept, but excluded from the recovery backlog until resolved explicitly, because<br/>
/// delivering them would fail on an endpoint that no longer has the handler.</summary>
[LongRunning]
public class Advertisement_shrink_tests : EndpointHostTestBase
{
   ///<summary>A remembered peer this specification plays the router for: <see cref="IPeerRegistry.RecordAdvertisementAsync"/> is<br/>
   /// exactly the seam a connection's advertisement fetch drives, so recording directly scripts the peer's advertisement<br/>
   /// lifecycle with no process behind it — the peer never connects, so nothing ever races delivery of what is bound to it.</summary>
   static readonly EndpointId NeverConnectedPeerId = new(Guid.Parse("5D3A97E2-8C41-4F0B-9D26-3B71C4E8A951"));

   [PCT] public async Task An_undelivered_tevent_bound_to_a_remembered_peer_whose_replaced_advertisement_renounces_its_subscription_leaves_the_recovery_backlog()
   {
      //The never-connected peer's first advertisement copies the Remote endpoint's, taggregate tevent subscriptions included.
      var remoteAdvertisement = BackendPeerRegistry.Peers.Single(peer => peer.Id.Equals(RemoteEndpointId)).HandledTessageTypes;
      await BackendPeerRegistry.RecordAdvertisementAsync(new EndpointInformation("NeverConnectedPeer", NeverConnectedPeerId, [..remoteAdvertisement]));

      //Publishes IMyTaggregateTevent exactly-once: fan-out reads the remembered subscribers, so the never-connected peer gets its undelivered row.
      await Navigator.PostAsync(MyCreateTaggregateTommand.Create());
      (await BackendOutboxSqlLayer.GetUndeliveredTessagesForEndpointAsync(NeverConnectedPeerId)).Must().HaveCount(1);

      //The peer's replaced advertisement renounces every subscription: the tevent lost its audience by that audience's own choice.
      await BackendPeerRegistry.RecordAdvertisementAsync(new EndpointInformation("NeverConnectedPeer", NeverConnectedPeerId, []));

      (await BackendOutboxSqlLayer.GetUndeliveredTessagesForEndpointAsync(NeverConnectedPeerId)).Must().BeEmpty();
   }

   [PCT] public async Task An_undelivered_tommand_bound_to_a_remembered_handler_whose_replaced_advertisement_no_longer_handles_its_type_leaves_the_recovery_backlog()
   {
      //The never-connected peer's first advertisement is the sole remembered handler of MyUnhandledExactlyOnceTommand - a type no real endpoint in this suite handles.
      var tommandTypeIdString = BackendEndPoint.ServiceLocator.Resolve<ITypeMap>().GetId(typeof(MyUnhandledExactlyOnceTommand)).CanonicalString;
      await BackendPeerRegistry.RecordAdvertisementAsync(new EndpointInformation("NeverConnectedPeer", NeverConnectedPeerId, [tommandTypeIdString]));

      //The send binds to the sole remembered handler - the never-connected peer - and waits for its return.
      await BackendEndPoint.ServiceLocator.Resolve<IIndependentTommandSender>().SendAsync(new MyUnhandledExactlyOnceTommand());
      (await BackendOutboxSqlLayer.GetUndeliveredTessagesForEndpointAsync(NeverConnectedPeerId)).Must().HaveCount(1);

      //The peer's replaced advertisement no longer handles the type: the tommand is stranded - kept, but never delivered to an endpoint that no longer has the handler.
      await BackendPeerRegistry.RecordAdvertisementAsync(new EndpointInformation("NeverConnectedPeer", NeverConnectedPeerId, []));

      (await BackendOutboxSqlLayer.GetUndeliveredTessagesForEndpointAsync(NeverConnectedPeerId)).Must().BeEmpty();
   }

   [PCT] public async Task A_tommand_bound_to_a_down_handler_that_returns_no_longer_handling_its_type_is_not_delivered_to_it_while_its_kept_tevent_subscriptions_are()
   {
      //The Backend met the Remote endpoint - the handler of the tommand's type - when the host started; rebuild the host without it: the handler is now down.
      await Host.DisposeAsync();
      await StartHostWithOnlyTheBackendEndpointAsync();

      //Bound at send to the remembered Remote endpoint - the sole remembered handler of the type.
      await BackendEndPoint.ServiceLocator.Resolve<IIndependentTommandSender>().SendAsync(new MyExactlyOnceTommandHandledByTheRemoteEndpoint());
      //Published while Remote is down: its kept taggregate subscription must still deliver on its return - the control proving recovery delivery runs.
      await Navigator.PostAsync(MyCreateTaggregateTommand.Create());

      await Host.DisposeAsyncWithoutWaitingForEndpointsToBeAtRest(); //Both tessages are undelivered: awaiting rest would wait for Remote's return.
      await StartHostWithTheRemoteEndpointReturningNoLongerHandlingItsTommandAsync();

      //The kept subscription's tevent delivers...
      MyRemoteTaggregateTeventHandlerThreadGate.AwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(15));
      //...while the tommand, whose type the returned advertisement no longer handles, is stranded rather than delivered to an
      //endpoint that would fail it - were it delivered, the failed handling would surface when the host disposes.
      MyExactlyOnceTommandHandledByTheRemoteEndpointHandlerThreadGate.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(2)).Must().BeFalse();
   }

   IPeerRegistry BackendPeerRegistry => BackendEndPoint.ServiceLocator.Resolve<IPeerRegistry>();
   ITessagingSqlLayer.IOutboxSqlLayer BackendOutboxSqlLayer => BackendEndPoint.ServiceLocator.Resolve<ITessagingSqlLayer.IOutboxSqlLayer>();
}
