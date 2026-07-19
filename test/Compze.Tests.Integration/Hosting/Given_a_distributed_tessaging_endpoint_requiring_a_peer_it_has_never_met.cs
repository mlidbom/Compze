using System.Collections.Concurrent;
using Compze.Tessaging.Endpoints;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Serialization.Newtonsoft.Wiring;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Tessaging.Abstractions;
using Compze.Tessaging.Engine;
using Compze.Tessaging.Implementation.Peers;
using Compze.Tests.Common.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;
using Compze.Threading.Testing;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Hosting;

///<summary>
/// Queue-before-first-contact on the distributed tier (see <c>src/Compze.Tessaging/dev_docs/peer-model.md</c>): a publisher that
/// requires a peer by <see cref="EndpointId"/> (<c>RequirePeers</c>) holds everything published before that peer's first
/// advertisement, and the subset matching the peer's subscriptions delivers, in order, when the peer is first met — so startup
/// ordering stops mattering and nothing a required peer should see is lost to the discovery race.
///</summary>
public class Given_a_distributed_tessaging_endpoint_requiring_a_peer_it_has_never_met : UniversalTestBase
{
   static readonly WaitTimeout HandlerTimeout = WaitTimeout.Seconds(30);
   static readonly EndpointId RequiredSubscriberEndpointId = new(Guid.Parse("94f7a3c8-1d5e-4b26-8c09-e57b20d84a61"));

   readonly IEndpointHost _publisherHost;
   readonly BestEffortEndpoint _publisherEndpoint;
   IEndpointHost? _subscriberHost;
   readonly AddressesOfTheLiveHosts _registry = new();

   readonly IThreadGate _subscriberTeventHandlerGate = IThreadGate.NewOpen(HandlerTimeout, "requiredSubscriberBestEffortTeventHandler");
   readonly ConcurrentQueue<IMyBestEffortTevent> _teventsHandledOnTheSubscriber = new();

   public Given_a_distributed_tessaging_endpoint_requiring_a_peer_it_has_never_met()
   {
      _publisherHost = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder());
      _publisherEndpoint = _publisherHost.RegisterEndpoint(container => BestEffortEndpoint.Build(
         container,
         "FirstContactPublisherEndpoint",
         new EndpointId(Guid.Parse("c85d19e7-4a2b-4f60-9d38-71b06c5f2ea4")),
         endpointBuilder => endpointBuilder
            .MapTypes(mapper => mapper.RegisterIntegrationTestTypeMappings())
            .TransportProtocol(registrar => registrar.CurrentTestsEndpointTransport())
            .NewtonsoftSerializer()
            .DiscoverEndpointsThrough(_registry)
            .RequirePeers(RequiredSubscriberEndpointId)));
   }

   protected override async Task InitializeAsyncInternal() => await _publisherHost.StartAsync().caf();

   protected override async Task DisposeAsyncInternal()
   {
      await _publisherHost.DisposeAsync().caf();
      if(_subscriberHost != null) await _subscriberHost.DisposeAsync().caf();
   }

   [PCT] public async Task tevents_published_before_the_required_peers_first_advertisement_are_delivered_to_it_in_order_on_its_arrival()
   {
      //The required peer does not exist yet - no process hosts it, no registry lists it - and the publishes still succeed:
      //everything is held for the declared EndpointId.
      1.Through(3).ForEach(PublishOnThePublisherEndpointInATransaction);

      //The required peer arrives, is discovered, and receives everything published before it existed - in publish order.
      _subscriberHost = CreateSubscriberHost();
      await _subscriberHost.StartAsync();
      _registry.Add(_subscriberHost);

      _subscriberTeventHandlerGate.AwaitPassedThroughCountEqualTo(3);
      _teventsHandledOnTheSubscriber.Select(it => it.SequenceNumber).SequenceEqual(1.Through(3)).Must().BeTrue();
   }

   [PCT] public async Task decommissioning_the_never_met_required_peer_ends_the_hold_and_the_peers_later_arrival_is_a_plain_first_contact()
   {
      1.Through(3).ForEach(PublishOnThePublisherEndpointInATransaction);

      //The act ends the first-contact hold, discarding the three held tevents - reported: the composition required a peer
      //that an administrator now declares is not coming.
      var report = await _publisherEndpoint.ServiceLocator.Resolve<IPeerAdministration>().DecommissionAsync(RequiredSubscriberEndpointId);
      report.Discarded.Single().Count.Must().Be(3);
      report.Discarded.Single().Description.Must().Contain("first contact");

      //Published after the decommission: held for nobody, delivered to nobody.
      PublishOnThePublisherEndpointInATransaction(sequenceNumber: 4);

      //When the peer arrives after all it is a plain first contact: it receives only what is published after it is met.
      _subscriberHost = CreateSubscriberHost();
      await _subscriberHost.StartAsync();
      _registry.Add(_subscriberHost);
      _registry.AwaitTwoReadsCompletingAfterNow(); //Two reads guarantee a full reconciliation pass ran: the connection is up, its delivery stream draining a fresh queue.
      PublishOnThePublisherEndpointInATransaction(sequenceNumber: 5);

      _subscriberTeventHandlerGate.AwaitPassedThroughCountEqualTo(1);
      _teventsHandledOnTheSubscriber.Select(it => it.SequenceNumber).Single().Must().Be(5);
   }

   IEndpointHost CreateSubscriberHost()
   {
      var host = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder());
      host.RegisterEndpoint(container => BestEffortEndpoint.Build(
         container,
         "FirstContactRequiredSubscriberEndpoint",
         RequiredSubscriberEndpointId,
         endpointBuilder =>
         {
            endpointBuilder
               .MapTypes(mapper => mapper.RegisterIntegrationTestTypeMappings())
               .TransportProtocol(registrar => registrar.CurrentTestsEndpointTransport())
               .NewtonsoftSerializer()
               .RegisterTessageHandlers(handle => handle.ForTevent((IMyBestEffortTevent tevent) =>
                {
                   _teventsHandledOnTheSubscriber.Enqueue(tevent);
                   _subscriberTeventHandlerGate.AwaitPassThrough();
                }));
         }));
      return host;
   }

   void PublishOnThePublisherEndpointInATransaction(int sequenceNumber) =>
      _publisherEndpoint.ServiceLocator.Resolve<IScopeFactory>().ExecuteUnitOfWork(unitOfWork =>
         unitOfWork.Resolve<IUnitOfWorkTeventPublisher>().Publish(new MyBestEffortTevent { SequenceNumber = sequenceNumber }));
}
