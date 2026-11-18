using System.Collections.Concurrent;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Must;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.TessageBus;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tests.Integration.InProcess;
using Compze.Threading;
using Compze.Threading.Testing;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Tessaging;

///<summary><see cref="ITeventObservation"/> in an endpoint's composition: resolved from the endpoint's container, it creates<br/>
/// dynamic observers as it does in a plain container's, of the tevent types the endpoint subscribed to for them with<br/>
/// <see cref="EndpointBuilder.SubscribeForDynamicObservers{TTevent}"/>, and refuses an observer of any other type, even of<br/>
/// tevents the endpoint itself publishes.</summary>
public class Given_an_exactly_once_endpoint_with_a_dynamic_observer : UniversalTestBase
{
#pragma warning disable CA2213 // Disposed in DisposeAsyncInternal through the base lifecycle, which the analyzer cannot see
   readonly TestingEndpointHost _host;
#pragma warning restore CA2213
   readonly ExactlyOnceEndpoint _endpoint;
   readonly ConcurrentQueue<IMyGreetingRequestedTevent> _observedTevents = [];
   readonly IThreadGate _observerGate = IThreadGate.NewOpen(WaitTimeout.Seconds(10), nameof(_observerGate));
   readonly MySpecialGreetingRequestedTevent _publishedTevent = new();

   public Given_an_exactly_once_endpoint_with_a_dynamic_observer()
   {
      _host = TestingEndpointHost.Create();
      _endpoint = _host.RegisterEndpoint(new EndpointSubscribingToGreetingsForDynamicObservers());
   }

   protected override async Task InitializeAsyncInternal()
   {
      await _host.StartAsync();
      _endpoint.ServiceLocator.Resolve<ITeventObservation>().ObserveTevents(observe => observe.ForTevent((IMyGreetingRequestedTevent tevent) =>
      {
         _observedTevents.Enqueue(tevent);
         _observerGate.AwaitPassThrough();
      }));
      _endpoint.ServiceLocator.Resolve<IScopeFactory>().ExecuteUnitOfWork(unitOfWork => unitOfWork.Resolve<IUnitOfWorkTeventPublisher>().Publish(_publishedTevent));
      await _observerGate.AwaitPassedThroughCountEqualToAsync(1);
   }

   protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync();

   [PCT] public void the_observer_observes_a_tevent_committed_in_the_endpoint() => _observedTevents.Single().Must().ReferenceEqual(_publishedTevent);

   [PCT] public void creating_a_dynamic_observer_of_a_tevent_type_the_endpoint_did_not_subscribe_for_dynamic_observers_throws_naming_the_type() =>
      Invoking(() => CreateDynamicObservers(observe => observe.ForTevent((IMyNumberedTevent _) => {})))
         .Must().Throw<Exception>()
         .Which.Message.Must().Contain(nameof(IMyNumberedTevent));

   [PCT] public void creating_a_dynamic_observer_of_a_tevent_type_the_endpoint_did_not_subscribe_for_dynamic_observers_throws_saying_how_to_subscribe_with_SubscribeForDynamicObservers() =>
      Invoking(() => CreateDynamicObservers(observe => observe.ForTevent((IMyNumberedTevent _) => {})))
         .Must().Throw<Exception>()
         .Which.Message.Must().Contain($"endpoint.{nameof(EndpointBuilder.SubscribeForDynamicObservers)}<{nameof(IMyNumberedTevent)}>()");

   [PCT] public void creating_a_dynamic_observer_of_each_unit_of_work_of_a_tevent_type_the_endpoint_did_not_subscribe_for_dynamic_observers_throws_naming_the_type() =>
      Invoking(() => CreateDynamicObservers(observe => observe.ForEachUnitOfWork((IReadOnlyList<IMyNumberedTevent> _) => {})))
         .Must().Throw<Exception>()
         .Which.Message.Must().Contain(nameof(IMyNumberedTevent));

   IDisposable CreateDynamicObservers(Action<TeventObservationRegistrar> observe) => _endpoint.ServiceLocator.Resolve<ITeventObservation>().ObserveTevents(observe);

   class EndpointSubscribingToGreetingsForDynamicObservers : ExactlyOnceEndpointDeclaration<EndpointSubscribingToGreetingsForDynamicObservers>, IEndpointIdentity
   {
      public static string Name => "DynamicObserving";
      public static EndpointId Id => new(Guid.Parse("7C3E91B4-5A28-4F6D-9E17-2B84D0C6A953"));

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireIntegrationTestTypeMappings();

      protected override void Declare(ExactlyOnceEndpointBuilder endpoint) => endpoint.SubscribeForDynamicObservers<IMyGreetingRequestedTevent>();
   }
}
