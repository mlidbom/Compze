using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Hosting;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.Testing;
using Compze.Must;

using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Engine;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;
using Compze.Threading.Testing;

using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Hosting;

///<summary>
/// An exactly-once endpoint that — deliberately — declares no discovery registry: it discovers nothing and connects to no
/// other endpoint, but it starts, serves, and converses with itself. A tommand the endpoint sends that its own roster serves
/// executes inline, through the engine's one executor, in the sender's execution — the consistency law: in-boundary is
/// immediate and transactional, so the handling is exactly-once by construction (one transaction, no delivery machinery
/// involved) and its failure fails the sender's execution. The process-manager pattern — handling one tessage sends a
/// follow-up tommand belonging to the same endpoint — is in-boundary composition, needing no discovery and no wire. The host
/// is the production host and the endpoint is composed explicitly (<see cref="ExactlyOnceEndpoint.Compose"/>), so the
/// composition stands entirely on what it declares.
///</summary>
public class Given_an_exactly_once_tessaging_endpoint_declaring_no_discovery_registry : UniversalTestBase
{
   static readonly WaitTimeout HandlerTimeout = WaitTimeout.Seconds(30);

   readonly IEndpointHost _host;
   readonly ExactlyOnceEndpoint _endpoint;
   readonly IThreadGate _inRosterTommandHandlerGate = IThreadGate.NewOpen(HandlerTimeout, "inRosterTommandHandler");

   public Given_an_exactly_once_tessaging_endpoint_declaring_no_discovery_registry()
   {
      _host = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder()
                                                          ._mutate(it => it.Registrar.CurrentTestsDbPoolIfNotCloneContainer()));

      _endpoint = _host.RegisterEndpoint(container => ExactlyOnceEndpoint.Compose(
         container,
         "EndpointDeclaringNoDiscoveryRegistry",
         new EndpointId(Guid.Parse("5b7e2f4a-9c81-4c56-8a3d-e1f60b924d7c")),
         endpoint =>
         {
            endpoint.MapTypes(mapper => mapper.RegisterIntegrationTestTypeMappings());
            endpoint.TransportProtocol(registrar => registrar.CurrentTestsEndpointTransport());
            endpoint.Database(registrar => registrar.CurrentTestsConfiguredSqlLayer(connectionStringName: endpoint.Configuration.Id.ToString()));
            endpoint.RegisterTessageHandlers(handle => handle
                       .ForTommand((TommandTheEndpointSendsItself _) =>
                        {
                           _inRosterTommandHandlerGate.AwaitPassThrough();
                           return Task.CompletedTask;
                        })
                       .ForTommand((TommandWhoseHandlerFails _, IUnitOfWorkResolver _) => Task.FromException(new InRosterTommandHandlerFailure())));
         }));
   }

   protected override async Task InitializeAsyncInternal() => await _host.StartAsync().caf();

   protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync().caf();

   [PCT] public void the_endpoint_starts_and_runs() => _endpoint.IsRunning.Must().BeTrue();

   [PCT] public void a_tommand_the_endpoint_sends_that_its_own_roster_serves_has_executed_inline_in_the_senders_unit_of_work_when_the_send_returns()
   {
      _endpoint.ServiceLocator.Resolve<IIndependentTommandSender>().Send(new TommandTheEndpointSendsItself());

      //No waiting: inline execution is synchronous with the send, so the handler has already run.
      _inRosterTommandHandlerGate.Passed.Must().Be(1);
   }

   [PCT] public void a_failing_in_roster_tommand_handler_fails_the_senders_execution() =>
      Invoking(() => _endpoint.ServiceLocator.Resolve<IIndependentTommandSender>().Send(new TommandWhoseHandlerFails()))
         .Must().Throw<InRosterTommandHandlerFailure>();

   protected internal class TommandTheEndpointSendsItself : TessageTypes.Remotable.ExactlyOnce.Tommand;
   protected internal class TommandWhoseHandlerFails : TessageTypes.Remotable.ExactlyOnce.Tommand;
   class InRosterTommandHandlerFailure : Exception;
}
