using Compze.Tessaging.TessageBus.Exceptions;
using System.Collections.Concurrent;
using System.Transactions;
using Compze.Tessaging.Endpoints;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Serialization.Newtonsoft.Wiring;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.SystemCE.TransactionsCE.Testing;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.Endpoints.BestEffort;
using Compze.Tessaging.Engine.HandlerRegistration.TessageHandlers;
using Compze.Tessaging.TessageBus.Internal.BestEffortDelivery;
using Compze.Tessaging.Peers;
using Compze.Tessaging.Peers.Internal;
using Compze.Tests.Common.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;
using Compze.Threading.Testing;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Hosting;

///<summary>
/// Queue-while-down on the distributed tier (see <c>src/Compze.Tessaging/dev_docs/peer-model.md</c>): a publisher that has met a
/// subscribing peer keeps its tevents for it while the peer is down — in memory, in order, nothing persisted anywhere — and the
/// peer's next connection drains them on its return. The subscriber lives in its own host so it can go down and return while
/// the publisher stays up; identity is the subscriber's <see cref="EndpointId"/>, which its next incarnation keeps.
///</summary>
public class Given_a_met_distributed_tessaging_subscriber_that_goes_down : UniversalTestBase
{
   static readonly WaitTimeout HandlerTimeout = WaitTimeout.Seconds(30);
   static readonly EndpointId SubscriberEndpointId = new(Guid.Parse("7b7e51d5-2e1a-4e0c-9c11-6c0f5b1d2a43"));

   readonly IEndpointHost _publisherHost;
   readonly BestEffortEndpoint _publisherEndpoint;
   IEndpointHost _subscriberHost;
   readonly AddressesOfTheLiveHosts _registry = new();

   //Shared across the subscriber's incarnations: whichever incarnation receives a tevent records it here, so the assertion reads
   //one uninterrupted sequence across the downtime.
   readonly IThreadGate _subscriberTeventHandlerGate = IThreadGate.NewOpen(HandlerTimeout, "subscriberBestEffortTeventHandler");
   readonly ConcurrentQueue<IMyBestEffortTevent> _teventsHandledOnTheSubscriber = new();

   public Given_a_met_distributed_tessaging_subscriber_that_goes_down()
   {
      _publisherHost = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder());
      _publisherEndpoint = _publisherHost.RegisterEndpoint(container => BestEffortEndpoint.Build(
         container,
         "QueueWhileDownPublisherEndpoint",
         new EndpointId(Guid.Parse("21c2e6a4-9b0f-4c3d-8a57-3f1de08b6c92")),
         endpointBuilder => endpointBuilder
            .RegisterComponents(registrar => registrar.RequireIntegrationTestTypeMappings())
            .TransportProtocol(registrar => registrar.CurrentTestsEndpointTransport())
            .NewtonsoftSerializer()
            .DiscoverEndpointsThrough(_registry)));

      _subscriberHost = CreateSubscriberHost();
   }

   IEndpointHost CreateSubscriberHost()
   {
      var host = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder());
      host.RegisterEndpoint(container => BestEffortEndpoint.Build(
         container,
         "QueueWhileDownSubscriberEndpoint",
         SubscriberEndpointId,
         endpointBuilder =>
         {
            endpointBuilder
               .RegisterComponents(registrar => registrar.RequireIntegrationTestTypeMappings())
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
      await MeetTheSubscriberDeliveringTeventOneThenTakeItDownAsync();

      2.Through(4).ForEach(PublishOnThePublisherEndpointInATransaction);

      //Return: a new incarnation of the subscriber - same EndpointId, new process-equivalent host - and the queue drains to it.
      _subscriberHost = CreateSubscriberHost();
      await _subscriberHost.StartAsync();
      _registry.Add(_subscriberHost);

      _subscriberTeventHandlerGate.AwaitPassedThroughCountEqualTo(4);
      _teventsHandledOnTheSubscriber.Select(it => it.SequenceNumber).SequenceEqual(1.Through(4)).Must().BeTrue();
   }

   [PCT] public async Task decommissioning_the_down_subscriber_discards_its_queued_tevents_and_its_later_return_is_a_first_contact()
   {
      await MeetTheSubscriberDeliveringTeventOneThenTakeItDownAsync();

      2.Through(4).ForEach(PublishOnThePublisherEndpointInATransaction);

      //The act discards the three queued tevents, reported - decommissioning a peer with held tessages is loud and deliberate.
      var report = await _publisherEndpoint.ServiceLocator.Resolve<IPeerAdministration>().DecommissionAsync(SubscriberEndpointId);
      report.Discarded.Single().Count.Must().Be(3);

      //Published while decommissioned: fanned out to nobody - nothing anywhere remembers the peer.
      PublishOnThePublisherEndpointInATransaction(sequenceNumber: 5);

      //The returned subscriber is a first contact again: it receives only what is published after it is re-met.
      _subscriberHost = CreateSubscriberHost();
      await _subscriberHost.StartAsync();
      _registry.Add(_subscriberHost);
      _registry.AwaitTwoReadsCompletingAfterNow(); //Two reads guarantee a full reconciliation pass ran: the connection is up, its delivery stream draining a fresh queue.
      PublishOnThePublisherEndpointInATransaction(sequenceNumber: 6);

      _subscriberTeventHandlerGate.AwaitPassedThroughCountEqualTo(2);
      _teventsHandledOnTheSubscriber.Select(it => it.SequenceNumber).SequenceEqual([1, 6]).Must().BeTrue();
   }

   [PCT] public async Task the_publish_that_would_exceed_10_000_queued_tevents_for_the_down_subscriber_fails_loud_naming_the_peer_and_the_bound()
   {
      await MeetTheSubscriberDeliveringTeventOneThenTakeItDownAsync();

      _publisherEndpoint.ServiceLocator.Resolve<IScopeFactory>().ExecuteUnitOfWork(unitOfWork =>
      {
         var publisher = unitOfWork.Resolve<IUnitOfWorkTeventPublisher>();
         //Slots are reserved at publish, inside the transaction, so the bound is hit before anything commits: these
         //10,000 publishes fill the subscriber's queue bound exactly...
         2.Through(10_001).ForEach(sequenceNumber => publisher.Publish(new MyBestEffortTevent { SequenceNumber = sequenceNumber }));

         //...and the publish that would exceed it fails loud - backpressure naming the peer and the bound, never silent shedding.
         var message = Invoking(() => publisher.Publish(new MyBestEffortTevent { SequenceNumber = 10_002 }))
                      .Must().Throw<BestEffortTeventQueueOverflowException>()
                      .Which.Message;
         message.Must().Contain(SubscriberEndpointId.ToString());
         message.Must().Contain("10000");
      });
   }

   [PCT] public async Task a_unit_of_work_that_rolls_back_releases_the_queue_slots_its_publishes_reserved()
   {
      await MeetTheSubscriberDeliveringTeventOneThenTakeItDownAsync();

      //A unit of work reserving the subscriber's entire queue bound rolls back...
      Invoking(() => _publisherEndpoint.ServiceLocator.Resolve<IScopeFactory>().ExecuteUnitOfWork(unitOfWork =>
                    {
                       Transaction.Current!.FailOnPrepare();
                       var publisher = unitOfWork.Resolve<IUnitOfWorkTeventPublisher>();
                       2.Through(10_001).ForEach(sequenceNumber => publisher.Publish(new MyBestEffortTevent { SequenceNumber = sequenceNumber }));
                    }))
                   .Must().Throw<TransactionAbortedException>();

      //...and its reservations die with it: a fresh unit of work can fill the whole bound again.
      _publisherEndpoint.ServiceLocator.Resolve<IScopeFactory>().ExecuteUnitOfWork(unitOfWork =>
      {
         var publisher = unitOfWork.Resolve<IUnitOfWorkTeventPublisher>();
         2.Through(10_001).ForEach(sequenceNumber => publisher.Publish(new MyBestEffortTevent { SequenceNumber = sequenceNumber }));
      });
   }

   ///<summary>The shared given: the publisher meets the subscriber (proven by tevent 1 arriving), then the subscriber goes<br/>
   /// down — it leaves the registry while still serving, so the publisher's router drops the connection cleanly and no delivery<br/>
   /// can be in flight at a failure moment; only then does the subscriber's host go away.</summary>
   async Task MeetTheSubscriberDeliveringTeventOneThenTakeItDownAsync()
   {
      AwaitThePublisherRememberingTheSubscriber();
      PublishOnThePublisherEndpointInATransaction(sequenceNumber: 1);
      _subscriberTeventHandlerGate.AwaitPassedThroughCountEqualTo(1);

      _registry.Remove(_subscriberHost);
      _registry.AwaitTwoReadsCompletingAfterNow(); //Two reads guarantee one full reconciliation pass ran against the shrunk membership: the connection is dropped.
      await _subscriberHost.DisposeAsync();
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

}
