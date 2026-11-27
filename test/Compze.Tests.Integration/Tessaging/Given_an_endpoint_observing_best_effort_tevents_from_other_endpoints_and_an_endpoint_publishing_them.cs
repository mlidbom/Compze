using System.Collections.Concurrent;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Must;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.TessageBus;
using Compze.Tests.Common.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;
using Compze.Threading.Testing;

#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Tessaging;

///<summary>What an endpoint's dynamic observers observe from other endpoints is subscribed to where it is composed, with<br/>
/// <see cref="EndpointBuilder.SubscribeForDynamicObservers{TTevent}"/>: the observing endpoint declares nothing else, and its peer<br/>
/// routes the subscribed tevents to it from the start. So a dynamic observer created right before a publish on the other<br/>
/// endpoint observes it, with no round trip between.</summary>
public class Given_an_endpoint_observing_best_effort_tevents_from_other_endpoints_and_an_endpoint_publishing_them : UniversalTestBase
{
#pragma warning disable CA2213 // Disposed in DisposeAsyncInternal through the base lifecycle, which the analyzer cannot see
   readonly TestingEndpointHost _host;
#pragma warning restore CA2213
   readonly ExactlyOnceEndpoint _publishingEndpoint;
   readonly ExactlyOnceEndpoint _observingEndpoint;

   protected Given_an_endpoint_observing_best_effort_tevents_from_other_endpoints_and_an_endpoint_publishing_them()
   {
      _host = TestingEndpointHost.Create();
      _publishingEndpoint = _host.RegisterEndpoint(new PublishingEndpointDeclaration());
      _observingEndpoint = _host.RegisterEndpoint(new ObservingEndpointDeclaration());
   }

   protected override async Task InitializeAsyncInternal()
   {
      await _host.StartAsync();
      await _host.AwaitEndpointsHaveMetEachOtherAsync();
   }

   protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync();

   ///<summary>A dynamic observer on the observing endpoint, which records the sequence number of each tevent it observes<br/>
   /// and passes through <paramref name="gate"/>.</summary>
   IDisposable ObserveOnTheObservingEndpoint(ConcurrentQueue<int> observedSequenceNumbers, IThreadGate gate) =>
      _observingEndpoint.ServiceLocator.Resolve<IDynamicTeventObservers>().ObserveTevents(observe => observe.ForTevent((IMyBestEffortTevent tevent) =>
      {
         observedSequenceNumbers.Enqueue(tevent.SequenceNumber);
         gate.AwaitPassThrough();
      }));

   void PublishOnThePublishingEndpoint(int sequenceNumber) =>
      _publishingEndpoint.ServiceLocator.Resolve<IScopeFactory>().ExecuteUnitOfWork(unitOfWork =>
         unitOfWork.Resolve<IUnitOfWorkTeventPublisher>().Publish(new MyBestEffortTevent { SequenceNumber = sequenceNumber }));

   public class with_a_dynamic_observer_created_right_before_tevent_1_is_published : Given_an_endpoint_observing_best_effort_tevents_from_other_endpoints_and_an_endpoint_publishing_them
   {
      ConcurrentQueue<int> ObservedSequenceNumbers { get; } = [];
      protected IThreadGate ObserverGate { get; } = IThreadGate.NewOpen(WaitTimeout.Seconds(10), nameof(ObserverGate));
      IDisposable Observation { get; set; } = null!;

      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         //No wait between creating the observer and publishing: the observing endpoint's peer already routes the subscribed tevents to it.
         Observation = ObserveOnTheObservingEndpoint(ObservedSequenceNumbers, ObserverGate);
         PublishOnThePublishingEndpoint(1);
         await ObserverGate.AwaitPassedThroughCountEqualToAsync(1);
      }

      [PCT] public void it_observes_tevent_1() => ObservedSequenceNumbers.Must().SequenceEqual([1]);

      public class after_it_is_disposed_and_a_second_dynamic_observer_observes_tevent_2 : with_a_dynamic_observer_created_right_before_tevent_1_is_published
      {
         readonly ConcurrentQueue<int> _observedBySecondObserver = [];
         readonly IThreadGate _secondObserverGate = IThreadGate.NewOpen(WaitTimeout.Seconds(10), nameof(_secondObserverGate));

         protected override async Task InitializeAsyncInternal()
         {
            await base.InitializeAsyncInternal();
            Observation.Dispose();
            ObserveOnTheObservingEndpoint(_observedBySecondObserver, _secondObserverGate);
            PublishOnThePublishingEndpoint(2);
            //Tevent 2 is queued for the dynamic observers as it arrives, so once the second observer has observed it, the disposed
            //one had been left out of the arrival's queueing.
            await _secondObserverGate.AwaitPassedThroughCountEqualToAsync(1);
         }

         [PCT] public void the_disposed_observer_observed_only_tevent_1() => ObservedSequenceNumbers.Must().SequenceEqual([1]);

         [PCT] public void the_second_observer_observed_tevent_2() => _observedBySecondObserver.Must().SequenceEqual([2]);
      }
   }

   class PublishingEndpointDeclaration : ExactlyOnceEndpointDeclaration<PublishingEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "PublishingBestEffort";
      public static EndpointId Id => new(Guid.Parse("2E8B5C71-94D3-4A6F-B0E2-7C13F58A9D46"));

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireIntegrationTestTypeMappings();
   }

   class ObservingEndpointDeclaration : ExactlyOnceEndpointDeclaration<ObservingEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "ObservingFromOtherEndpoints";
      public static EndpointId Id => new(Guid.Parse("D61F2A84-3B7E-4C95-8E0A-41B6C27F935D"));

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireIntegrationTestTypeMappings();

      protected override void Declare(ExactlyOnceEndpointBuilder endpoint) => endpoint.SubscribeForDynamicObservers<IMyBestEffortTevent>();
   }
}
