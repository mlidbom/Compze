using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Tessaging.EndpointLifecycle;

///<summary>Stopping an endpoint (<see cref="IEndpoint.StopAsync"/>) is the mirror of starting it, and a step of its own<br/>
/// before disposal: a stopped endpoint serves nothing and has released its process lock, while its container keeps serving<br/>
/// until the endpoint is disposed — so a caller whose components are bound to one thread stops first, then disposes from<br/>
/// that thread.</summary>
public class Given_a_started_exactly_once_endpoint : UniversalTestBase
{
   readonly IDependencyInjectionContainer _rootContainer;
   readonly TestingEndpointHost _host;
   readonly ExactlyOnceEndpoint _endpoint;

   public Given_a_started_exactly_once_endpoint()
   {
      //One root container for every host the specifications create: the database pool lives in it, so an endpoint with the
      //same identity on a second host joins the same domain database - the one the process lock is held in.
      _rootContainer = TestEnv.DIContainer.CreateTestingContainerBuilder()
                              ._mutate(it => it.Registrar.CurrentTestsDbPoolIfNotCloneContainer())
                              .Build();
      _host = TestingEndpointHost.Create(_rootContainer);
      _endpoint = _host.RegisterEndpoint(new StoppedEndpointDeclaration());
   }

   class StoppedEndpointDeclaration : ExactlyOnceEndpointDeclaration<StoppedEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "Stopped";
      public static EndpointId Id => new(Guid.Parse("C3E8A1F4-6B2D-4E97-A05C-81D4F37B29E6"));

      protected override void RegisterComponents(IComponentRegistrar registrar)
      {
         registrar.RequireIntegrationTestTypeMappings();
         registrar.Register(Singleton.For<DisposalThreadRecorder>().CreatedBy(() => new DisposalThreadRecorder()));
      }
   }

   ///<summary>A component of the endpoint's container that records the thread its disposal runs on.</summary>
   sealed class DisposalThreadRecorder : IDisposable
   {
      public int? DisposingThreadId { get; private set; }
      public void Dispose() => DisposingThreadId = Environment.CurrentManagedThreadId;
   }

   protected override async Task InitializeAsyncInternal() => await _host.StartAsync();

   protected override async Task DisposeAsyncInternal()
   {
      await _host.DisposeAsync();
      await _rootContainer.DisposeAsync();
   }

   public class after_it_stops : Given_a_started_exactly_once_endpoint
   {
      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         await _endpoint.StopAsync();
      }

      [PCT] public void it_is_no_longer_running() => _endpoint.IsRunning.Must().BeFalse();

      [PCT] public void it_has_no_address() => _endpoint.Address.Must().BeNull();

      [PCT] public void its_container_still_resolves_a_component_not_yet_disposed() =>
         _endpoint.ServiceLocator.Resolve<DisposalThreadRecorder>().DisposingThreadId.Must().BeNull();

      [PCT] public async Task starting_it_again_fails_loud_stating_that_a_stopped_endpoint_never_starts_again() =>
         (await InvokingAsync(async () => await _endpoint.StartAsync()).Must().ThrowAsync<InvalidOperationException>())
           .Which.Message.Must().Contain("a stopped endpoint never starts again");

      public class and_an_endpoint_with_the_same_identity_then_starts_on_another_host : after_it_stops
      {
         readonly TestingEndpointHost _otherHost;
         readonly ExactlyOnceEndpoint _endpointOnTheOtherHost;

         public and_an_endpoint_with_the_same_identity_then_starts_on_another_host()
         {
            _otherHost = TestingEndpointHost.Create(_rootContainer);
            _endpointOnTheOtherHost = _otherHost.RegisterEndpoint(new StoppedEndpointDeclaration());
         }

         protected override async Task InitializeAsyncInternal()
         {
            await base.InitializeAsyncInternal();
            await _otherHost.StartAsync();
         }

         protected override async Task DisposeAsyncInternal()
         {
            await _otherHost.DisposeAsync();
            await base.DisposeAsyncInternal();
         }

         //The stopped endpoint is not yet disposed: a start refused with EndpointAlreadyRunningInAnotherProcessException here
         //would mean the lock waits for disposal.
         [PCT] public void that_endpoint_runs_because_stopping_released_the_process_lock() => _endpointOnTheOtherHost.IsRunning.Must().BeTrue();
      }

      ///<summary>The scenario stopping exists for: a UI thread blocking on the disposal of an endpoint whose container holds<br/>
      /// components bound to it. The container disposes the component it created last first, so the recorder - resolved after<br/>
      /// the stop, created last - shows which thread the container's disposal began on.</summary>
      public class and_is_then_disposed_by_a_thread_blocking_on_the_disposal_after_resolving_a_disposable_component : after_it_stops
      {
         DisposalThreadRecorder _recorder = null!;
         int _disposingThreadId;

         protected override async Task InitializeAsyncInternal()
         {
            await base.InitializeAsyncInternal();
            _recorder = _endpoint.ServiceLocator.Resolve<DisposalThreadRecorder>();
            _disposingThreadId = await Task.Run(() =>
            {
               _endpoint.DisposeAsync().AsTask().GetAwaiter().GetResult();
               return Environment.CurrentManagedThreadId;
            });
         }

         [PCT] public void the_component_is_disposed_on_the_disposing_thread() => _recorder.DisposingThreadId.Must().Be(_disposingThreadId);
      }
   }
}
