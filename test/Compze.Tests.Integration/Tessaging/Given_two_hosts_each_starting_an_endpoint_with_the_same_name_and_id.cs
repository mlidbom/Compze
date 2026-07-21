using Compze.Tessaging.Endpoints;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.Endpoints.Exceptions;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Tessaging;

///<summary>An endpoint runs in exactly one process at a time, enforced by the endpoint catalog's process lock in the domain<br/>
/// database: the lock is exclusivity a live holder holds — never a time-bounded lease — so while an instance runs, a second<br/>
/// instance claiming the same endpoint is refused immediately and loudly at startup, naming the endpoint and the holder<br/>
/// (<see cref="EndpointAlreadyRunningInAnotherProcessException"/>), under any load: no pause or slowdown can make the<br/>
/// holder look dead. The endpoints share one domain database because the testing host keys the database by the<br/>
/// <see cref="EndpointId"/>.</summary>
public class Given_two_hosts_each_starting_an_endpoint_with_the_same_name_and_id : UniversalTestBase
{
   static readonly EndpointId ContestedEndpointId = new(Guid.Parse("7D4B2E19-8A6C-4F53-9B07-E12A85C4D3F6"));

   readonly IDependencyInjectionContainer _rootContainer;
   readonly TestingEndpointHost _firstHost;
   readonly ExactlyOnceEndpoint _runningEndpoint;

   public Given_two_hosts_each_starting_an_endpoint_with_the_same_name_and_id()
   {
      //One root container shared by both hosts: the database pool lives in it, so both endpoints' declarations resolve the
      //endpoint id's connection string to the one domain database the lock is contested in - the host-rebuild idiom's setup.
      _rootContainer = TestEnv.DIContainer.CreateTestingContainerBuilder()
                              ._mutate(it => it.Registrar.CurrentTestsDbPoolIfNotCloneContainer())
                              .Build();
      _firstHost = TestingEndpointHost.Create(_rootContainer);
      _runningEndpoint = RegisterTheContestedEndpointWith(_firstHost);
   }

   static ExactlyOnceEndpoint RegisterTheContestedEndpointWith(TestingEndpointHost host) =>
      host.RegisterExactlyOnceEndpoint(
         "Contested",
         ContestedEndpointId,
         endpointBuilder => endpointBuilder.RegisterComponents(registrar => registrar.RequireIntegrationTestTypeMappings()));

   protected override async Task InitializeAsyncInternal() => await _firstHost.StartAsync();

   protected override async Task DisposeAsyncInternal()
   {
      await _firstHost.DisposeAsync();
      await _rootContainer.DisposeAsync();
   }

   [PCT] public async Task the_second_start_fails_loud_naming_the_endpoint_and_the_live_holder_and_the_running_endpoint_is_unaffected()
   {
      var secondHost = TestingEndpointHost.Create(_rootContainer);
      RegisterTheContestedEndpointWith(secondHost);

      //The host reports its endpoints' start failures as one AggregateException - several endpoints can fail one start phase.
      var startFailure = (await InvokingAsync(async () => await secondHost.StartAsync())
                            .Must().ThrowAsync<AggregateException>()).Which;
      var lockRefusal = startFailure.Flatten().InnerExceptions.Single();
      (lockRefusal is EndpointAlreadyRunningInAnotherProcessException).Must().BeTrue();
      lockRefusal.Message.Must().Contain("Contested")
                 .Contain("already running in another process")
                 .Contain("held by process");

      _runningEndpoint.IsRunning.Must().BeTrue();

      //The claimant never took anything: disposing it must not disturb the running endpoint, whose own clean disposal in
      //teardown - no lock-was-lost background exception - is the proof the lock stayed with its live holder.
      await secondHost.DisposeAsync();
      _runningEndpoint.IsRunning.Must().BeTrue();
   }
}
