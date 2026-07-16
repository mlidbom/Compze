using Compze.Abstractions.Hosting.Public;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Must;

using Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Tessaging.Hosting;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.InProcess;

///<summary>
/// An endpoint composes its Tessaging from the in-process core the transport-speaking layers extend: every
/// feature declaration is idempotent, exactly-once Tessaging contains distributed Tessaging contains in-process
/// Tessaging so declaring several composes in either order, and registering tessaging handlers is
/// order-independent of every feature declaration. Whether a tevent crosses the wire is a property of the
/// tevent's type, honored by the delivery legs the composition wires — not an endpoint-wide mode.
///</summary>
public class Given_an_endpoint_composing_its_tessaging_features : UniversalTestBase
{
   readonly ITestingEndpointHost _host = TestingEndpointHost.Create();

   protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync().caf();

   IEndpoint RegisterEndpointWith(string name, Action<IEndpointBuilder> setup) =>
      _host.RegisterEndpoint(name, new EndpointId(Guid.NewGuid()), setup);

   [PCT] public void declaring_in_process_then_exactly_once_tessaging_composes_since_exactly_once_tessaging_contains_the_in_process_core() =>
      RegisterEndpointWith("InProcessThenExactlyOnce", builder =>
      {
         DeclareTheEndpointsFoundation(builder);
         builder.AddInProcessTessaging();
         builder.AddExactlyOnceTessaging();
      }).Must().NotBeNull();

   [PCT] public void declaring_exactly_once_then_in_process_tessaging_composes_since_exactly_once_tessaging_contains_the_in_process_core() =>
      RegisterEndpointWith("ExactlyOnceThenInProcess", builder =>
      {
         DeclareTheEndpointsFoundation(builder);
         builder.AddExactlyOnceTessaging();
         builder.AddInProcessTessaging();
      }).Must().NotBeNull();

   [PCT] public void registering_tessaging_handlers_before_declaring_in_process_tessaging_succeeds() =>
      RegisterEndpointWith("HandlersThenInProcess", builder =>
      {
         builder.RegisterTessagingHandlers.ForTevent((IMyGreetingRequestedTevent _) => {});
         builder.AddInProcessTessaging();
      }).Must().NotBeNull();

   [PCT] public void registering_tessaging_handlers_before_declaring_exactly_once_tessaging_succeeds() =>
      RegisterEndpointWith("HandlersThenExactlyOnce", builder =>
      {
         DeclareTheEndpointsFoundation(builder);
         builder.RegisterTessagingHandlers.ForTevent((IMyGreetingRequestedTevent _) => {});
         builder.AddExactlyOnceTessaging();
      }).Must().NotBeNull();

   [PCT] public void declaring_in_process_tessaging_twice_is_idempotent() =>
      RegisterEndpointWith("InProcessTwice", builder =>
      {
         builder.AddInProcessTessaging();
         builder.AddInProcessTessaging();
      }).Must().NotBeNull();

   [PCT] public void declaring_exactly_once_tessaging_twice_is_idempotent() =>
      RegisterEndpointWith("ExactlyOnceTwice", builder =>
      {
         DeclareTheEndpointsFoundation(builder);
         builder.AddExactlyOnceTessaging();
         builder.AddExactlyOnceTessaging();
      }).Must().NotBeNull();

   ///<summary>Exactly-once Tessaging asserts that the endpoint's foundation — transport protocol, persistence, serializer — is<br/>
   /// declared before the feature is added. These specifications are about composing the Tessaging features, so they declare the<br/>
   /// current test's foundation without exercising it. (The serializer comes from the testing host's root container.)</summary>
   static void DeclareTheEndpointsFoundation(IEndpointBuilder builder) =>
      builder.Registrar.CurrentTestsEndpointTransport()
                       .CurrentTestsConfiguredSqlLayer(connectionStringName: builder.Configuration.Id.ToString());
}
