using System.Collections.Concurrent;
using Compze.Tessaging.Endpoints;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.Endpoints.BestEffort;
using Compze.Tessaging.Peers;
using Compze.Tests.Common.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;
using Compze.Threading.Testing;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Hosting;

///<summary>
/// The per-peer opt-down from queue-while-down (<c>DoNotQueueTeventsFor</c> — see
/// <c>src/Compze.Tessaging/dev_docs/peers.md</c>): ephemerality is a property of the relationship, so an endpoint that queues
/// for the peers it depends on can still declare, peer by peer, that it keeps nothing for one it does not care about. Tevents
/// for such a peer are delivered only while it is connected: published while it is down, they are dropped, and the peer resumes
/// from tevents published after its return.
///</summary>
public class Given_a_met_distributed_tessaging_subscriber_the_publisher_does_not_queue_tevents_for : UniversalTestBase
{
   static readonly WaitTimeout HandlerTimeout = WaitTimeout.Seconds(30);

   readonly IEndpointHost _publisherHost;
   readonly BestEffortEndpoint _publisherEndpoint;
   IEndpointHost _subscriberHost;
   readonly AddressesOfTheLiveHosts _registry = new();

   readonly IThreadGate _subscriberTeventHandlerGate = IThreadGate.NewOpen(HandlerTimeout, "notQueuedForSubscriberBestEffortTeventHandler");
   readonly ConcurrentQueue<IMyBestEffortTevent> _teventsHandledOnTheSubscriber = new();

   public Given_a_met_distributed_tessaging_subscriber_the_publisher_does_not_queue_tevents_for()
   {
      _publisherHost = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder(), new CurrentTestsBestEffortEnvironment(_registry));
      _publisherEndpoint = _publisherHost.RegisterEndpoint(new PublisherEndpointDeclaration());

      _subscriberHost = CreateSubscriberHost();
   }

   IEndpointHost CreateSubscriberHost()
   {
      var host = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder(), new CurrentTestsBestEffortEnvironment());
      host.RegisterEndpoint(new SubscriberEndpointDeclaration(this));
      return host;
   }

   class PublisherEndpointDeclaration : BestEffortEndpointDeclaration<PublisherEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "NoQueueingPublisherEndpoint";
      public static EndpointId Id { get; } = new(Guid.Parse("E47B06D2-3C95-48A1-BF60-27D8C41E95B0"));

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireIntegrationTestTypeMappings();

      ///<summary>The opt-down itself: the publisher declares, peer by peer, that it keeps nothing for this one.</summary>
      protected override IReadOnlyList<EndpointId> PeersNotQueuedFor => [SubscriberEndpointDeclaration.Id];
   }

   class SubscriberEndpointDeclaration : BestEffortEndpointDeclaration<SubscriberEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "NoQueueingSubscriberEndpoint";
      public static EndpointId Id { get; } = new(Guid.Parse("5A92C4E1-7F3B-4D08-B6C5-90E12A8D47F3"));

      readonly Given_a_met_distributed_tessaging_subscriber_the_publisher_does_not_queue_tevents_for _specification;
      internal SubscriberEndpointDeclaration(Given_a_met_distributed_tessaging_subscriber_the_publisher_does_not_queue_tevents_for specification) => _specification = specification;

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireIntegrationTestTypeMappings();

      protected override void RegisterBestEffortTeventHandlers(IBestEffortTeventHandlerRegistrar handle) => handle
         .ForTevent((IMyBestEffortTevent tevent) =>
          {
             _specification._teventsHandledOnTheSubscriber.Enqueue(tevent);
             _specification._subscriberTeventHandlerGate.AwaitPassThrough();
          });
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

   [PCT] public async Task tevents_published_while_the_subscriber_is_down_are_dropped_and_it_resumes_from_tevents_published_after_its_return()
   {
      AwaitThePublisherRememberingTheSubscriber();
      PublishOnThePublisherEndpointInATransaction(sequenceNumber: 1);
      _subscriberTeventHandlerGate.AwaitPassedThroughCountEqualTo(1);

      //Down, cleanly: the subscriber leaves the registry while still serving, then its host goes away.
      _registry.Remove(_subscriberHost);
      _registry.AwaitTwoReadsCompletingAfterNow(); //A full reconciliation pass ran against the shrunk membership: the connection is dropped.
      await _subscriberHost.DisposeAsync();

      //The opt-down: these are dropped - the publisher declared it keeps nothing for this peer.
      2.Through(4).ForEach(PublishOnThePublisherEndpointInATransaction);

      _subscriberHost = CreateSubscriberHost();
      await _subscriberHost.StartAsync();
      _registry.Add(_subscriberHost);
      _registry.AwaitTwoReadsCompletingAfterNow(); //A full reconciliation pass ran against the grown membership: the connection is up and its stream is draining.

      PublishOnThePublisherEndpointInATransaction(sequenceNumber: 5);

      _subscriberTeventHandlerGate.AwaitPassedThroughCountEqualTo(2);
      _teventsHandledOnTheSubscriber.Select(it => it.SequenceNumber).SequenceEqual([1, 5]).Must().BeTrue();
   }

   void PublishOnThePublisherEndpointInATransaction(int sequenceNumber) =>
      _publisherEndpoint.ServiceLocator.Resolve<IScopeFactory>().ExecuteUnitOfWork(unitOfWork =>
         unitOfWork.Resolve<IUnitOfWorkTeventPublisher>().Publish(new MyBestEffortTevent { SequenceNumber = sequenceNumber }));

   ///<summary>Even a not-queued-for peer must be met before fan-out targets it at all, so the specification waits until the<br/>
   /// subscriber appears in the publisher's peer registry.</summary>
   void AwaitThePublisherRememberingTheSubscriber()
   {
      var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
      while(!_publisherEndpoint.ServiceLocator.Resolve<IPeerAdministration>().Peers.Any(peer => peer.Id.Equals(SubscriberEndpointDeclaration.Id)))
      {
         if(DateTime.UtcNow > deadline) throw new TimeoutException("The publisher never met the subscriber: it never appeared in the publisher's peer registry.");
         Thread.Sleep(20);
      }
   }
}
