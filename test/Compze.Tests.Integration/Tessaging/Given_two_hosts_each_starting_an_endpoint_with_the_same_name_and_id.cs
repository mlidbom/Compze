using Compze.Tessaging.Endpoints;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Underscore;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Tessaging;

///<summary>An endpoint runs in exactly one process at a time, enforced by the endpoint catalog's process lease in the domain<br/>
/// database: while a live instance holds the lease — proving it alive through its heartbeats — a second instance claiming<br/>
/// the same endpoint waits out one lease duration and then fails loud at startup, naming the endpoint and the holder<br/>
/// (<see cref="EndpointAlreadyRunningInAnotherProcessException"/>). The endpoints share one domain database because the<br/>
/// testing host keys the database by the <see cref="EndpointId"/>.</summary>
public class Given_two_hosts_each_starting_an_endpoint_with_the_same_name_and_id : UniversalTestBase
{
   static readonly EndpointId ContestedEndpointId = new(Guid.Parse("7D4B2E19-8A6C-4F53-9B07-E12A85C4D3F6"));
   //Long enough that a heartbeat delayed by suite load never fakes the holder's death, short enough that the claimant's
   //bounded wait (one lease duration plus margin) keeps the specification fast.
   static readonly TimeSpan LeaseDuration = TimeSpan.FromSeconds(2);

   readonly IDependencyInjectionContainer _rootContainer;
   readonly TestingEndpointHost _firstHost;
   readonly ExactlyOnceEndpoint _runningEndpoint;

   public Given_two_hosts_each_starting_an_endpoint_with_the_same_name_and_id()
   {
      //One root container shared by both hosts: the database pool lives in it, so both endpoints' declarations resolve the
      //endpoint id's connection string to the one domain database the lease is contested in - the host-rebuild idiom's setup.
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
         endpointBuilder => endpointBuilder
            .MapTypes(mapper => mapper.RegisterIntegrationTestTypeMappings())
            .ProcessLeaseDuration(LeaseDuration));

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
      var leaseRefusal = startFailure.Flatten().InnerExceptions.Single();
      (leaseRefusal is EndpointAlreadyRunningInAnotherProcessException).Must().BeTrue();
      leaseRefusal.Message.Must().Contain("Contested")
                  .Contain("already running in another process")
                  .Contain("held by process");

      _runningEndpoint.IsRunning.Must().BeTrue();

      //The claimant never took anything: disposing it must not disturb the running endpoint, whose own clean disposal in
      //teardown - no lease-was-taken background exception - is the proof the lease stayed with its live holder.
      await secondHost.DisposeAsync();
      _runningEndpoint.IsRunning.Must().BeTrue();
   }
}
