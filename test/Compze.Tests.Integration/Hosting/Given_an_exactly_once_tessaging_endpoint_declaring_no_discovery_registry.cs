using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Endpoints;
using Compze.Hosting;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tessaging.TessageTypes;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;
using Compze.Threading.Testing;

using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Hosting;

///<summary>
/// An exactly-once endpoint whose environment — deliberately — declares no discovery registry: it discovers nothing and
/// connects to no other endpoint, but it starts, serves, and converses with itself. A tommand the endpoint sends that its own
/// roster serves executes inline, through the engine's one executor, in the sender's execution — the consistency law:
/// in-boundary is immediate and transactional, so the handling is exactly-once by construction (one transaction, no delivery
/// machinery involved) and its failure fails the sender's execution. The process-manager pattern — handling one tessage sends
/// a follow-up tommand belonging to the same endpoint — is in-boundary composition, needing no discovery and no wire. The
/// host is the production host and the environment is the specification's own: everything the endpoint is comes from its
/// declaration and its environment.
///</summary>
public class Given_an_exactly_once_tessaging_endpoint_declaring_no_discovery_registry : UniversalTestBase
{
   static readonly WaitTimeout HandlerTimeout = WaitTimeout.Seconds(30);

   readonly IEndpointHost _host;
   readonly ExactlyOnceEndpoint _endpoint;
   readonly IThreadGate _inRosterTommandHandlerGate = IThreadGate.NewOpen(HandlerTimeout, "inRosterTommandHandler");
   object? _serviceResolvedIntoTheTommandHandler;

   public Given_an_exactly_once_tessaging_endpoint_declaring_no_discovery_registry()
   {
      _host = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder()
                                                          ._mutate(it => it.Registrar.CurrentTestsDbPoolIfNotCloneContainer()),
                                             new EnvironmentDeclaringNoDiscoveryRegistry());

      _endpoint = _host.RegisterEndpoint(new NoDiscoveryRegistryEndpointDeclaration(this));
   }

   ///<summary>This specification's point, as an environment: no discovery registry — just the current test's transport and<br/>
   /// the current test's domain-database binding keyed by the endpoint's id.</summary>
   class EnvironmentDeclaringNoDiscoveryRegistry : IEndpointEnvironment
   {
      public void Configure(EndpointBuilder endpointBuilder) =>
         endpointBuilder.TransportProtocol(registrar => registrar.CurrentTestsEndpointTransport());

      public void ConfigureDomainDatabase(ExactlyOnceEndpointBuilder endpointBuilder) =>
         endpointBuilder.ConfigurePersistence(registrar => registrar.CurrentTestsConfiguredSqlLayer(connectionStringName: endpointBuilder.Configuration.Id.ToString()));
   }

   class NoDiscoveryRegistryEndpointDeclaration : ExactlyOnceEndpointDeclaration<NoDiscoveryRegistryEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "NoDiscoveryRegistry";
      public static EndpointId Id { get; } = new(Guid.Parse("5B7E2F4A-9C81-4C56-8A3D-E1F60B924D7C"));

      readonly Given_an_exactly_once_tessaging_endpoint_declaring_no_discovery_registry _specification;
      internal NoDiscoveryRegistryEndpointDeclaration(Given_an_exactly_once_tessaging_endpoint_declaring_no_discovery_registry specification) => _specification = specification;

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar
         .RequireIntegrationTestTypeMappings()
         ._mutate(it => it.Register(Scoped.For<ServiceResolvedIntoTheTommandHandler>().CreatedBy(() => new ServiceResolvedIntoTheTommandHandler())));

      protected override void RegisterExactlyOnceTommandHandlers(IExactlyOnceTommandHandlerRegistrar handle) => handle
         .ForTommand((TommandTheEndpointSendsItself _) =>
          {
             _specification._inRosterTommandHandlerGate.AwaitPassThrough();
             return Task.CompletedTask;
          })
         .ForTommand((TommandWhoseHandlerResolvesADependency _, ServiceResolvedIntoTheTommandHandler resolved) =>
          {
             _specification._serviceResolvedIntoTheTommandHandler = resolved;
             return Task.CompletedTask;
          })
         .ForTommand((TommandWhoseHandlerFails _, IUnitOfWorkResolver _) => Task.FromException(new InRosterTommandHandlerFailure()));
   }

   protected override async Task InitializeAsyncInternal() => await _host.StartAsync().caf();

   protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync().caf();

   [PCT] public void the_endpoint_starts_and_runs() => _endpoint.IsRunning.Must().BeTrue();

   [PCT] public async Task a_tommand_the_endpoint_sends_that_its_own_roster_serves_has_executed_inline_in_the_senders_unit_of_work_when_the_send_returns()
   {
      await _endpoint.ServiceLocator.Resolve<IIndependentTommandSender>().SendAsync(new TommandTheEndpointSendsItself());

      //No waiting: inline execution is synchronous with the send, so the handler has already run.
      _inRosterTommandHandlerGate.Passed.Must().Be(1);
   }

   [PCT] public async Task a_failing_in_roster_tommand_handler_fails_the_senders_execution() =>
      await InvokingAsync(async () => await _endpoint.ServiceLocator.Resolve<IIndependentTommandSender>().SendAsync(new TommandWhoseHandlerFails()))
         .Must().ThrowAsync<InRosterTommandHandlerFailure>();

   [PCT] public async Task a_tommand_handler_declared_through_the_with_dependency_overload_receives_its_dependency_resolved()
   {
      await _endpoint.ServiceLocator.Resolve<IIndependentTommandSender>().SendAsync(new TommandWhoseHandlerResolvesADependency());

      //No waiting: inline execution is synchronous with the send, so the handler has already run.
      _serviceResolvedIntoTheTommandHandler.Must().NotBeNull();
   }

   protected internal class TommandTheEndpointSendsItself : Remotable.ExactlyOnce.Tommand;
   protected internal class TommandWhoseHandlerResolvesADependency : Remotable.ExactlyOnce.Tommand;
   protected internal class TommandWhoseHandlerFails : Remotable.ExactlyOnce.Tommand;
   class InRosterTommandHandlerFailure : Exception;
   class ServiceResolvedIntoTheTommandHandler;
}
