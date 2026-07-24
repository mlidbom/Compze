using System.Collections.Concurrent;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting;
using Compze.Hosting.SameMachine;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Underscore;
using Compze.Internals.Testing;
using Compze.Internals.Testing.Utilities.Awaiting;
using Compze.Must;
using Compze.Tessaging._private.Routing;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.BestEffort;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tessaging.TessageBus;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;
using Compze.Threading.Testing;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tessaging.InternalSpecifications;

///<summary>
/// The per-peer opt-down from queue-while-down (<c>DoNotQueueTeventsFor</c> — see
/// <c>src/Compze.Tessaging/dev_docs/peers.md</c>): ephemerality is a property of the relationship, so an endpoint that queues
/// for the peers it depends on can still declare, peer by peer, that it keeps nothing for one it does not care about. Tevents
/// for such a peer are delivered only while it is connected: published while it is down, they are dropped, and the peer resumes
/// from tevents published after its return.
///</summary>
///<remarks>An internal specification, and honestly so. Every step of it is defined by whether the publisher's router currently
/// holds a connection to the subscriber — that is what "while it is down" means for a peer nothing is queued for — and a consumer
/// has no way to ask that question, nor any reason to: the delivery model exists so that applications never have to. They declare
/// what they need through waiting sends, <c>RequirePeers</c> and readiness, and the connection stays the framework's business.
/// So this specification asks <see cref="ITessagingRouter.HasLiveConnectionTo"/> directly. Its predecessor lived among the
/// black-box specifications and paid for the pretence: unable to ask, it waited on proxies — the peer being <em>remembered</em>,
/// which happens before the connection exists, and a count of reads of a stand-in registry — and failed intermittently because
/// neither proxy meant what the specification needed it to mean.</remarks>
public class Given_a_met_distributed_tessaging_subscriber_the_publisher_does_not_queue_tevents_for : UniversalTestBase
{
   static readonly WaitTimeout HandlerTimeout = WaitTimeout.Seconds(30);
   static DirectoryInfo TestDirectory => new DirectoryInfo(Path.Combine(Path.GetTempPath(), "Compze", "Tests", "EndpointRegistry"))._mutate(it => it.Create());

   readonly InterprocessEndpointRegistry _registry;
   readonly IEndpointHost _publisherHost;
   readonly BestEffortEndpoint _publisherEndpoint;
   IEndpointHost _subscriberHost;

   readonly IThreadGate _subscriberTeventHandlerGate = IThreadGate.NewOpen(HandlerTimeout, "notQueuedForSubscriberBestEffortTeventHandler");
   readonly ConcurrentQueue<ISequencedBestEffortTevent> _teventsHandledOnTheSubscriber = new();

   public Given_a_met_distributed_tessaging_subscriber_the_publisher_does_not_queue_tevents_for()
   {
      _registry = InterprocessEndpointRegistry.OpenOrCreateSessionLocal(Guid.NewGuid().ToString(), TestDirectory);

      _publisherHost = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder(), new EnvironmentParticipatingInTheSpecificationsRegistry(_registry));
      _publisherEndpoint = _publisherHost.RegisterEndpoint(new PublisherEndpointDeclaration());

      _subscriberHost = CreateSubscriberHost();
   }

   IEndpointHost CreateSubscriberHost()
   {
      var host = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder(), new EnvironmentParticipatingInTheSpecificationsRegistry(_registry));
      host.RegisterEndpoint(new SubscriberEndpointDeclaration(this));
      return host;
   }

   class PublisherEndpointDeclaration : BestEffortEndpointDeclaration<PublisherEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "NoQueueingPublisherEndpoint";
      public static EndpointId Id { get; } = new(Guid.Parse("E47B06D2-3C95-48A1-BF60-27D8C41E95B0"));

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireTessagingInternalSpecificationTypeMappings();

      ///<summary>The opt-down itself: the publisher declares, peer by peer, that it keeps nothing for this one.</summary>
      protected override IReadOnlyList<EndpointId> PeersNotQueuedFor => [SubscriberEndpointDeclaration.Id];
   }

   class SubscriberEndpointDeclaration : BestEffortEndpointDeclaration<SubscriberEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "NoQueueingSubscriberEndpoint";
      public static EndpointId Id { get; } = new(Guid.Parse("5A92C4E1-7F3B-4D08-B6C5-90E12A8D47F3"));

      readonly Given_a_met_distributed_tessaging_subscriber_the_publisher_does_not_queue_tevents_for _specification;
      internal SubscriberEndpointDeclaration(Given_a_met_distributed_tessaging_subscriber_the_publisher_does_not_queue_tevents_for specification) => _specification = specification;

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireTessagingInternalSpecificationTypeMappings();

      protected override void RegisterBestEffortTeventHandlers(IBestEffortTeventHandlerRegistrar handle) => handle
         .ForTevent((ISequencedBestEffortTevent tevent) =>
          {
             _specification._teventsHandledOnTheSubscriber.Enqueue(tevent);
             _specification._subscriberTeventHandlerGate.AwaitPassThrough();
          });
   }

   protected override async Task InitializeAsyncInternal()
   {
      await _publisherHost.StartAsync().caf();
      await _subscriberHost.StartAsync().caf();
   }

   protected override async Task DisposeAsyncInternal()
   {
      await _publisherHost.DisposeAsync().caf();
      await _subscriberHost.DisposeAsync().caf();
      _registry.Delete();
      _registry.Dispose();
   }

   [PCT] public async Task tevents_published_while_the_subscriber_is_down_are_dropped_and_it_resumes_from_tevents_published_after_its_return()
   {
      AwaitThePublisherConnectedToTheSubscriber();
      PublishOnThePublisherEndpointInATransaction(sequenceNumber: 1);
      _subscriberTeventHandlerGate.AwaitPassedThroughCountEqualTo(1);

      //Down, cleanly: the subscriber leaves the registry while still serving, then its host goes away.
      _registry.RetractEndpointAddress(SubscriberEndpointDeclaration.Id);
      AwaitThePublisherDisconnectedFromTheSubscriber();
      await _subscriberHost.DisposeAsync();

      //The opt-down: these are dropped - the publisher declared it keeps nothing for this peer.
      2.Through(4).ForEach(PublishOnThePublisherEndpointInATransaction);

      _subscriberHost = CreateSubscriberHost();
      await _subscriberHost.StartAsync();
      AwaitThePublisherConnectedToTheSubscriber();

      PublishOnThePublisherEndpointInATransaction(sequenceNumber: 5);

      _subscriberTeventHandlerGate.AwaitPassedThroughCountEqualTo(2);
      _teventsHandledOnTheSubscriber.Select(it => it.SequenceNumber).SequenceEqual([1, 5]).Must().BeTrue();
   }

   void PublishOnThePublisherEndpointInATransaction(int sequenceNumber) =>
      _publisherEndpoint.ServiceLocator.Resolve<IScopeFactory>().ExecuteUnitOfWork(unitOfWork =>
         unitOfWork.Resolve<IUnitOfWorkTeventPublisher>().Publish(new SequencedBestEffortTevent { SequenceNumber = sequenceNumber }));

   ///<summary>A tevent for a not-queued-for peer is delivered only while the connection is up, so every step of this specification
   /// begins by waiting for the connection to be in the state the step is about.</summary>
   void AwaitThePublisherConnectedToTheSubscriber() =>
      PublishersRouter.PollAwait(it => it.HasLiveConnectionTo(SubscriberEndpointDeclaration.Id));

   void AwaitThePublisherDisconnectedFromTheSubscriber() =>
      PublishersRouter.PollAwait(it => !it.HasLiveConnectionTo(SubscriberEndpointDeclaration.Id));

   ITessagingRouter PublishersRouter => _publisherEndpoint.ServiceLocator.Resolve<ITessagingRouter>();
}
