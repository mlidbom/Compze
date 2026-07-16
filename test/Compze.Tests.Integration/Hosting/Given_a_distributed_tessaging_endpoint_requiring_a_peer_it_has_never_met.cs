using System.Collections.Concurrent;
using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Serialization.Newtonsoft.Wiring;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Tessaging.Hosting;
using Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;
using Compze.Threading.Testing;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Hosting;

///<summary>
/// Queue-before-first-contact on the distributed tier (see <c>dev_docs/TODO/durable-peer-topology.md</c>): a publisher that
/// requires a peer by <see cref="EndpointId"/> (<c>RequirePeers</c>) holds everything published before that peer's first
/// advertisement, and the subset matching the peer's subscriptions delivers, in order, when the peer is first met — so startup
/// ordering stops mattering and nothing a required peer should see is lost to the discovery race.
///</summary>
public class Given_a_distributed_tessaging_endpoint_requiring_a_peer_it_has_never_met : UniversalTestBase
{
   static readonly WaitTimeout HandlerTimeout = WaitTimeout.Seconds(30);
   static readonly EndpointId RequiredSubscriberEndpointId = new(Guid.Parse("94f7a3c8-1d5e-4b26-8c09-e57b20d84a61"));

   readonly IEndpointHost _publisherHost;
   readonly IEndpoint _publisherEndpoint;
   IEndpointHost? _subscriberHost;
   readonly AddressesOfTheLiveHosts _registry = new();

   readonly IThreadGate _subscriberTeventHandlerGate = IThreadGate.NewOpen(HandlerTimeout, "requiredSubscriberBestEffortTeventHandler");
   readonly ConcurrentQueue<IMyBestEffortTevent> _teventsHandledOnTheSubscriber = new();

   public Given_a_distributed_tessaging_endpoint_requiring_a_peer_it_has_never_met()
   {
      _publisherHost = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder());
      _publisherEndpoint = _publisherHost.RegisterEndpoint(
         "FirstContactPublisherEndpoint",
         new EndpointId(Guid.Parse("c85d19e7-4a2b-4f60-9d38-71b06c5f2ea4")),
         builder =>
         {
            builder.TypeMapper.RegisterIntegrationTestTypeMappings();
            builder.ComposeFoundationWithCurrentTestsTransportAndNoDatabase()
                   .AddDistributedTessaging(tessaging => tessaging.NewtonsoftSerializer())
                   .DiscoverEndpointsThrough(_registry)
                   .RequirePeers(RequiredSubscriberEndpointId);
         });
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

   IEndpointHost CreateSubscriberHost()
   {
      var host = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder());
      host.RegisterEndpoint(
         "FirstContactRequiredSubscriberEndpoint",
         RequiredSubscriberEndpointId,
         builder =>
         {
            builder.TypeMapper.RegisterIntegrationTestTypeMappings();
            builder.ComposeFoundationWithCurrentTestsTransportAndNoDatabase()
                   .AddDistributedTessaging(tessaging => tessaging.NewtonsoftSerializer())
                   .RegisterHandlers(register => register.ForTevent((IMyBestEffortTevent tevent) =>
                    {
                       _teventsHandledOnTheSubscriber.Enqueue(tevent);
                       _subscriberTeventHandlerGate.AwaitPassThrough();
                    }));
         });
      return host;
   }

   void PublishOnThePublisherEndpointInATransaction(int sequenceNumber) =>
      _publisherEndpoint.ServiceLocator.Resolve<IScopeFactory>().ExecuteUnitOfWork(unitOfWork =>
         unitOfWork.Resolve<IUnitOfWorkTeventPublisher>().Publish(new MyBestEffortTevent { SequenceNumber = sequenceNumber }));

   ///<summary>Serves the live hosts' Tessaging addresses to the publisher's router — the discovery a production suite gets from a shared registry.</summary>
   class AddressesOfTheLiveHosts : IEndpointRegistry
   {
      readonly IMonitor _monitor = IMonitor.New();
      readonly List<IEndpointHost> _liveHosts = [];

      internal void Add(IEndpointHost host) => _monitor.Locked(() => _liveHosts.Add(host));

      public IEnumerable<EndpointAddress> ServerEndpointAddresses => _monitor.Locked(() =>
         (IReadOnlyList<EndpointAddress>)
         [
            .._liveHosts.SelectMany(host => host.Endpoints)
                        .Where(endpoint => endpoint.TessagingAddress is not null)
                        .Select(endpoint => endpoint.TessagingAddress!)
         ]);
   }
}
