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

using Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Tessaging.Hosting;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;
using Compze.Threading.Testing;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Hosting;

///<summary>
/// An endpoint composing the full exactly-once Tessaging pipeline that — deliberately — declares no discovery
/// registry: it discovers nothing and connects to no other endpoint, but it starts, serves, and self-sends.
/// Its router always maintains the one connection that needs no discovery — to its own inbox — because an
/// exactly-once tommand routes to whichever endpoint advertises its type, the sender itself included: a tommand
/// the endpoint sends that its own handlers serve rides the outbox → own-inbox pipeline with the same delivery
/// semantics as any other tommand (asynchronous, its own transaction, exactly-once) — the process-manager
/// pattern, where handling one tessage sends a follow-up tommand belonging to the same endpoint. The host is
/// the production host — nothing is pre-registered, so the composition stands entirely on what it declares.
///</summary>
public class Given_an_exactly_once_tessaging_endpoint_declaring_no_discovery_registry : UniversalTestBase
{
   static readonly WaitTimeout HandlerTimeout = WaitTimeout.Seconds(30);

   readonly IEndpointHost _host;
   readonly IEndpoint _endpoint;
   readonly IThreadGate _selfSentTommandHandlerGate = IThreadGate.NewOpen(HandlerTimeout, "selfSentTommandHandler");

   public Given_an_exactly_once_tessaging_endpoint_declaring_no_discovery_registry()
   {
      _host = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder()
                                                          ._mutate(it => it.Registrar.CurrentTestsDbPoolIfNotCloneContainer()));

      _endpoint = _host.RegisterEndpoint(
         "EndpointDeclaringNoDiscoveryRegistry",
         new EndpointId(Guid.Parse("5b7e2f4a-9c81-4c56-8a3d-e1f60b924d7c")),
         builder =>
         {
            builder.TypeMapper.RegisterIntegrationTestTypeMappings();
            builder.Registrar.CurrentTestsEndpointTransport()
                   .CurrentTestsConfiguredSqlLayer(connectionStringName: builder.Configuration.Id.ToString());
            builder.AddExactlyOnceTessaging()
                   .RegisterHandlers(register => register.ForTommand((TommandTheEndpointSendsItself _) => _selfSentTommandHandlerGate.AwaitPassThrough()));
         });
   }

   protected override async Task InitializeAsyncInternal() => await _host.StartAsync().caf();

   protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync().caf();

   [PCT] public void the_endpoint_starts_and_runs() => _endpoint.IsRunning.Must().BeTrue();

   [PCT] public void a_tommand_the_endpoint_sends_that_its_own_handler_serves_is_delivered_through_its_own_inbox()
   {
      _endpoint.ServiceLocator.Resolve<IIndependentTommandSender>().Send(new TommandTheEndpointSendsItself());

      _selfSentTommandHandlerGate.AwaitPassedThroughCountEqualTo(1);
   }

   protected internal class TommandTheEndpointSendsItself : TessageTypes.Remotable.ExactlyOnce.Tommand;
}
