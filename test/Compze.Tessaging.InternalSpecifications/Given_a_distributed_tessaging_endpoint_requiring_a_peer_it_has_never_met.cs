using System.Collections.Concurrent;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting;
using Compze.Hosting.SameMachine;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.Testing;
using Compze.Internals.Testing.Utilities.Awaiting;
using Compze.Must;
using Compze.Tessaging._private.Routing;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.BestEffort;
using Compze.Tessaging.Peers;
using Compze.Tessaging.TessageBus;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;
using Compze.Threading.Testing;
using Compze.Underscore;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tessaging.InternalSpecifications;

///<summary>
/// Queue-before-first-contact on the distributed tier (see <c>src/Compze.Tessaging/dev_docs/peers.md</c>): a publisher that
/// requires a peer by <see cref="EndpointId"/> (<c>RequirePeers</c>) holds everything published before that peer's first
/// advertisement, and the subset matching the peer's subscriptions delivers, in order, when the peer is first met — so startup
/// ordering stops mattering and nothing a required peer should see is lost to the discovery race.
///</summary>
///<remarks>An internal specification because the second specification below has to publish at a moment defined by the
/// publisher's router: after the returning peer's connection is up, so that what it receives proves the hold has ended rather
/// than that the hold delivered. Only <see cref="ITessagingRouter.HasLiveConnectionTo"/> says when that is — a consumer neither
/// can ask nor needs to, since <c>RequirePeers</c> exists precisely so applications need not think about the race.</remarks>
public class Given_a_distributed_tessaging_endpoint_requiring_a_peer_it_has_never_met : UniversalTestBase
{
   static readonly WaitTimeout HandlerTimeout = WaitTimeout.Seconds(30);
   static DirectoryInfo TestDirectory => new DirectoryInfo(Path.Combine(Path.GetTempPath(), "Compze", "Tests", "EndpointRegistry"))._mutate(it => it.Create());

   readonly InterprocessEndpointRegistry _registry;
   readonly IEndpointHost _publisherHost;
   readonly BestEffortEndpoint _publisherEndpoint;
   IEndpointHost? _subscriberHost;

   readonly IThreadGate _subscriberTeventHandlerGate = IThreadGate.NewOpen(HandlerTimeout, "requiredSubscriberBestEffortTeventHandler");
   readonly ConcurrentQueue<ISequencedBestEffortTevent> _teventsHandledOnTheSubscriber = new();

   public Given_a_distributed_tessaging_endpoint_requiring_a_peer_it_has_never_met()
   {
      _registry = InterprocessEndpointRegistry.OpenOrCreateSessionLocal(Guid.NewGuid().ToString(), TestDirectory);

      _publisherHost = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder(), new EnvironmentParticipatingInTheSpecificationsRegistry(_registry));
      _publisherEndpoint = _publisherHost.RegisterEndpoint(new PublisherEndpointDeclaration());
   }

   class PublisherEndpointDeclaration : BestEffortEndpointDeclaration<PublisherEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "FirstContactPublisherEndpoint";
      public static EndpointId Id { get; } = new(Guid.Parse("C85D19E7-4A2B-4F60-9D38-71B06C5F2EA4"));

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireTessagingInternalSpecificationTypeMappings();

      ///<summary>The requirement that opens the first-contact hold: everything published before this peer's first advertisement is held for it.</summary>
      protected override IReadOnlyList<EndpointId> RequiredPeers => [SubscriberEndpointDeclaration.Id];
   }

   class SubscriberEndpointDeclaration : BestEffortEndpointDeclaration<SubscriberEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "FirstContactRequiredSubscriberEndpoint";
      public static EndpointId Id { get; } = new(Guid.Parse("94F7A3C8-1D5E-4B26-8C09-E57B20D84A61"));

      readonly Given_a_distributed_tessaging_endpoint_requiring_a_peer_it_has_never_met _specification;
      internal SubscriberEndpointDeclaration(Given_a_distributed_tessaging_endpoint_requiring_a_peer_it_has_never_met specification) => _specification = specification;

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireTessagingInternalSpecificationTypeMappings();

      protected override void RegisterBestEffortTeventHandlers(IBestEffortTeventHandlerRegistrar handle) => handle
         .ForTevent((ISequencedBestEffortTevent tevent) =>
          {
             _specification._teventsHandledOnTheSubscriber.Enqueue(tevent);
             _specification._subscriberTeventHandlerGate.AwaitPassThrough();
          });
   }

   protected override async Task InitializeAsyncInternal() => await _publisherHost.StartAsync().caf();

   protected override async Task DisposeAsyncInternal()
   {
      await _publisherHost.DisposeAsync().caf();
      if(_subscriberHost != null) await _subscriberHost.DisposeAsync().caf();
      _registry.Delete();
      _registry.Dispose();
   }

   [PCT] public async Task tevents_published_before_the_required_peers_first_advertisement_are_delivered_to_it_in_order_on_its_arrival()
   {
      //The required peer does not exist yet - no process hosts it, no registry lists it - and the publishes still succeed:
      //everything is held for the declared EndpointId.
      1.Through(3).ForEach(PublishOnThePublisherEndpointInATransaction);

      //The required peer arrives, is discovered, and receives everything published before it existed - in publish order.
      _subscriberHost = CreateSubscriberHost();
      await _subscriberHost.StartAsync();

      _subscriberTeventHandlerGate.AwaitPassedThroughCountEqualTo(3);
      _teventsHandledOnTheSubscriber.Select(it => it.SequenceNumber).SequenceEqual(1.Through(3)).Must().BeTrue();
   }

   [PCT] public async Task decommissioning_the_never_met_required_peer_ends_the_hold_and_the_peers_later_arrival_is_a_plain_first_contact()
   {
      1.Through(3).ForEach(PublishOnThePublisherEndpointInATransaction);

      //The act ends the first-contact hold, discarding the three held tevents - reported: the composition required a peer
      //that an administrator now declares is not coming.
      var report = await _publisherEndpoint.ServiceLocator.Resolve<IPeerAdministration>().DecommissionAsync(SubscriberEndpointDeclaration.Id);
      report.Discarded.Single().Count.Must().Be(3);
      report.Discarded.Single().Description.Must().Contain("first contact");

      //Published after the decommission: held for nobody, delivered to nobody.
      PublishOnThePublisherEndpointInATransaction(sequenceNumber: 4);

      //When the peer arrives after all it is a plain first contact: it receives only what is published after it is met.
      _subscriberHost = CreateSubscriberHost();
      await _subscriberHost.StartAsync();
      AwaitThePublisherConnectedToTheSubscriber();
      PublishOnThePublisherEndpointInATransaction(sequenceNumber: 5);

      _subscriberTeventHandlerGate.AwaitPassedThroughCountEqualTo(1);
      _teventsHandledOnTheSubscriber.Select(it => it.SequenceNumber).Single().Must().Be(5);
   }

   IEndpointHost CreateSubscriberHost()
   {
      var host = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder(), new EnvironmentParticipatingInTheSpecificationsRegistry(_registry));
      host.RegisterEndpoint(new SubscriberEndpointDeclaration(this));
      return host;
   }

   void PublishOnThePublisherEndpointInATransaction(int sequenceNumber) =>
      _publisherEndpoint.ServiceLocator.Resolve<IScopeFactory>().ExecuteUnitOfWork(unitOfWork =>
         unitOfWork.Resolve<IUnitOfWorkTeventPublisher>().Publish(new SequencedBestEffortTevent { SequenceNumber = sequenceNumber }));

   ///<summary>Publishing "after the peer is met" means after the connection carrying its deliveries exists, so the specification
   /// waits for exactly that rather than for anything that merely precedes it.</summary>
   void AwaitThePublisherConnectedToTheSubscriber() =>
      PublishersRouter.PollAwait(it => it.HasLiveConnectionTo(SubscriberEndpointDeclaration.Id));

   ITessagingRouter PublishersRouter => _publisherEndpoint.ServiceLocator.Resolve<ITessagingRouter>();
}
