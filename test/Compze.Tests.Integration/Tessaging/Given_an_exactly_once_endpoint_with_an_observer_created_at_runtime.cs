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

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Tessaging;

///<summary><see cref="ITeventObservation"/> in an endpoint's composition: resolved from the endpoint's container, it creates<br/>
/// observers at runtime as it does in a plain container's.</summary>
public class Given_an_exactly_once_endpoint_with_an_observer_created_at_runtime : UniversalTestBase
{
#pragma warning disable CA2213 // Disposed in DisposeAsyncInternal through the base lifecycle, which the analyzer cannot see
   readonly TestingEndpointHost _host;
#pragma warning restore CA2213
   readonly ExactlyOnceEndpoint _endpoint;
   readonly ConcurrentQueue<IMyGreetingRequestedTevent> _observedTevents = [];
   readonly IThreadGate _observerGate = IThreadGate.NewOpen(WaitTimeout.Seconds(10), nameof(_observerGate));
   readonly MySpecialGreetingRequestedTevent _publishedTevent = new();

   public Given_an_exactly_once_endpoint_with_an_observer_created_at_runtime()
   {
      _host = TestingEndpointHost.Create();
      _endpoint = _host.RegisterEndpoint(new EndpointWithNoHandlersDeclaration());
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

   class EndpointWithNoHandlersDeclaration : ExactlyOnceEndpointDeclaration<EndpointWithNoHandlersDeclaration>, IEndpointIdentity
   {
      public static string Name => "RuntimeObserving";
      public static EndpointId Id => new(Guid.Parse("7C3E91B4-5A28-4F6D-9E17-2B84D0C6A953"));

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireIntegrationTestTypeMappings();
   }
}
