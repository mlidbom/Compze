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
using Compze.Tessaging.Implementation.Peers;
using Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;
using Compze.Threading.Testing;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Hosting;

///<summary>
/// Queue-while-down on the distributed tier (see <c>dev_docs/TODO/durable-peer-topology.md</c>): a publisher that has met a
/// subscribing peer keeps its tevents for it while the peer is down — in memory, in order, nothing persisted anywhere — and the
/// peer's next connection drains them on its return. The subscriber lives in its own host so it can go down and return while
/// the publisher stays up; identity is the subscriber's <see cref="EndpointId"/>, which its next incarnation keeps.
///</summary>
public class Given_a_met_distributed_tessaging_subscriber_that_goes_down : UniversalTestBase
{
   static readonly WaitTimeout HandlerTimeout = WaitTimeout.Seconds(30);
   static readonly EndpointId SubscriberEndpointId = new(Guid.Parse("7b7e51d5-2e1a-4e0c-9c11-6c0f5b1d2a43"));

   readonly IEndpointHost _publisherHost;
   readonly IEndpoint _publisherEndpoint;
   IEndpointHost _subscriberHost;
   readonly AddressesOfTheLiveHosts _registry = new();

   //Shared across the subscriber's incarnations: whichever incarnation receives a tevent records it here, so the assertion reads
   //one uninterrupted sequence across the downtime.
   readonly IThreadGate _subscriberTeventHandlerGate = IThreadGate.NewOpen(HandlerTimeout, "subscriberBestEffortTeventHandler");
   readonly ConcurrentQueue<IMyBestEffortTevent> _teventsHandledOnTheSubscriber = new();

   public Given_a_met_distributed_tessaging_subscriber_that_goes_down()
   {
      _publisherHost = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder());
      _publisherEndpoint = _publisherHost.RegisterEndpoint(
         "QueueWhileDownPublisherEndpoint",
         new EndpointId(Guid.Parse("21c2e6a4-9b0f-4c3d-8a57-3f1de08b6c92")),
         builder =>
         {
            builder.TypeMapper.RegisterIntegrationTestTypeMappings();
            builder.ComposeFoundationWithCurrentTestsTransportAndNoDatabase()
                   .AddDistributedTessaging(tessaging => tessaging.NewtonsoftSerializer())
                   .DiscoverEndpointsThrough(_registry);
         });

      _subscriberHost = CreateSubscriberHost();
   }

   IEndpointHost CreateSubscriberHost()
   {
      var host = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder());
      host.RegisterEndpoint(
         "QueueWhileDownSubscriberEndpoint",
         SubscriberEndpointId,
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

   protected override async Task InitializeAsyncInternal()
   {
      await _publisherHost.StartAsync().caf();
      await _subscriberHost.StartAsync().caf();
      _registry.Add(_subscriberHost);
   }

   protected override async Task DisposeAsyncInternal()
   {
      await _publisherHost.DisposeAsync().caf();
      await _subscriberHost.DisposeAsync().caf();
   }

   [PCT] public async Task tevents_published_while_the_subscriber_is_down_are_delivered_in_order_when_it_returns()
   {
      AwaitThePublisherRememberingTheSubscriber();
      PublishOnThePublisherEndpointInATransaction(sequenceNumber: 1);
      _subscriberTeventHandlerGate.AwaitPassedThroughCountEqualTo(1);

      //Down: the subscriber leaves the registry while still serving, so the publisher's router drops the connection cleanly -
      //no delivery can be in flight at a failure moment - and only then does the subscriber's host go away.
      _registry.Remove(_subscriberHost);
      _registry.AwaitTwoReadsCompletingAfterNow(); //Two reads guarantee one full reconciliation pass ran against the shrunk membership: the connection is dropped.
      await _subscriberHost.DisposeAsync();

      2.Through(4).ForEach(PublishOnThePublisherEndpointInATransaction);

      //Return: a new incarnation of the subscriber - same EndpointId, new process-equivalent host - and the queue drains to it.
      _subscriberHost = CreateSubscriberHost();
      await _subscriberHost.StartAsync();
      _registry.Add(_subscriberHost);

      _subscriberTeventHandlerGate.AwaitPassedThroughCountEqualTo(4);
      _teventsHandledOnTheSubscriber.Select(it => it.SequenceNumber).SequenceEqual(1.Through(4)).Must().BeTrue();
   }

   void PublishOnThePublisherEndpointInATransaction(int sequenceNumber) =>
      _publisherEndpoint.ServiceLocator.Resolve<IScopeFactory>().ExecuteUnitOfWork(unitOfWork =>
         unitOfWork.Resolve<IUnitOfWorkTeventPublisher>().Publish(new MyBestEffortTevent { SequenceNumber = sequenceNumber }));

   ///<summary>Queue-while-down holds tevents for a peer the publisher has met — publishing before first contact would truthfully<br/>
   /// deliver nothing — so the specification waits until the subscriber appears in the publisher's peer registry.</summary>
   void AwaitThePublisherRememberingTheSubscriber()
   {
      var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
      while(!_publisherEndpoint.ServiceLocator.Resolve<IPeerRegistry>().Peers.Any(peer => peer.Id.Equals(SubscriberEndpointId)))
      {
         if(DateTime.UtcNow > deadline) throw new TimeoutException("The publisher never met the subscriber: it never appeared in the publisher's peer registry.");
         Thread.Sleep(20);
      }
   }

   ///<summary>Serves the live hosts' Tessaging addresses to the publisher's router — the discovery a production suite gets from a<br/>
   /// shared registry — and counts the router's reads, so the specification can await the reconciliation pass that acts on a<br/>
   /// membership change instead of sleeping and hoping.</summary>
   class AddressesOfTheLiveHosts : IEndpointRegistry
   {
      readonly IMonitor _monitor = IMonitor.New();
      readonly List<IEndpointHost> _liveHosts = [];
      long _reads;

      internal void Add(IEndpointHost host) => _monitor.Locked(() => _liveHosts.Add(host));
      internal void Remove(IEndpointHost host) => _monitor.Locked(() => _liveHosts.Remove(host));

      public IEnumerable<EndpointAddress> ServerEndpointAddresses => _monitor.Locked(() =>
      {
         _reads++;
         return (IReadOnlyList<EndpointAddress>)
         [
            .._liveHosts.SelectMany(host => host.Endpoints)
                        .Where(endpoint => endpoint.TessagingAddress is not null)
                        .Select(endpoint => endpoint.TessagingAddress!)
         ];
      });

      ///<summary>Returns once two <see cref="ServerEndpointAddresses"/> reads have completed after the call: the first read may<br/>
      /// belong to a reconciliation pass that was already mid-flight on the old membership, but the second belongs to a pass that<br/>
      /// started afterwards — so by then a full pass has acted on the current membership.</summary>
      internal void AwaitTwoReadsCompletingAfterNow()
      {
         var readsAtCall = _monitor.Locked(() => _reads);
         var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
         while(_monitor.Locked(() => _reads) < readsAtCall + 2)
         {
            if(DateTime.UtcNow > deadline) throw new TimeoutException("The publisher's router stopped reading the registry.");
            Thread.Sleep(20);
         }
      }
   }
}
