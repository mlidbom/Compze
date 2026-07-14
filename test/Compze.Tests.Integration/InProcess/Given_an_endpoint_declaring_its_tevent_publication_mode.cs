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
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.InProcess;

///<summary>
/// An endpoint declares its tevent publication mode exactly once — in-process-only or distributed, never
/// both — while registering tessaging handlers declares no mode at all, so handler registration is
/// order-independent of the mode declaration.
///</summary>
public class Given_an_endpoint_declaring_its_tevent_publication_mode : UniversalTestBase
{
   readonly ITestingEndpointHost _host = TestingEndpointHost.Create();

   protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync().caf();

   IEndpoint RegisterEndpointWith(string name, Action<IEndpointBuilder> setup) =>
      _host.RegisterEndpoint(name, new EndpointId(Guid.NewGuid()), setup);

   [PCT] public void declaring_in_process_then_distributed_tessaging_fails_stating_a_tevent_publication_mode_is_already_declared() =>
      Invoking(() => RegisterEndpointWith("InProcessThenDistributed", builder =>
              {
                 builder.AddInProcessTessaging();
                 builder.AddDistributedTessaging();
              }))
         .Must().Throw<Exception>().Which.Message.Must().Contain("A tevent publication mode is already declared");

   [PCT] public void declaring_distributed_then_in_process_tessaging_fails_stating_a_tevent_publication_mode_is_already_declared() =>
      Invoking(() => RegisterEndpointWith("DistributedThenInProcess", builder =>
              {
                 DeclareTheEndpointsFoundation(builder);
                 builder.AddDistributedTessaging();
                 builder.AddInProcessTessaging();
              }))
         .Must().Throw<Exception>().Which.Message.Must().Contain("A tevent publication mode is already declared");

   [PCT] public void registering_tessaging_handlers_before_declaring_in_process_tessaging_succeeds() =>
      RegisterEndpointWith("HandlersThenInProcess", builder =>
      {
         builder.RegisterTessagingHandlers.ForTevent((IMyGreetingRequestedTevent _) => {});
         builder.AddInProcessTessaging();
      }).Must().NotBeNull();

   [PCT] public void registering_tessaging_handlers_before_declaring_distributed_tessaging_succeeds() =>
      RegisterEndpointWith("HandlersThenDistributed", builder =>
      {
         DeclareTheEndpointsFoundation(builder);
         builder.RegisterTessagingHandlers.ForTevent((IMyGreetingRequestedTevent _) => {});
         builder.AddDistributedTessaging();
      }).Must().NotBeNull();

   [PCT] public void declaring_in_process_tessaging_twice_is_idempotent() =>
      RegisterEndpointWith("InProcessTwice", builder =>
      {
         builder.AddInProcessTessaging();
         builder.AddInProcessTessaging();
      }).Must().NotBeNull();

   [PCT] public void declaring_distributed_tessaging_twice_is_idempotent() =>
      RegisterEndpointWith("DistributedTwice", builder =>
      {
         DeclareTheEndpointsFoundation(builder);
         builder.AddDistributedTessaging();
         builder.AddDistributedTessaging();
      }).Must().NotBeNull();

   ///<summary>Distributed Tessaging asserts that the endpoint's foundation — transport protocol, persistence, serializer — is<br/>
   /// declared before the feature is added. These specifications are about the tevent publication mode, so they declare the<br/>
   /// current test's foundation without exercising it. (The serializer comes from the testing host's root container.)</summary>
   static void DeclareTheEndpointsFoundation(IEndpointBuilder builder) =>
      builder.Registrar.CurrentTestsEndpointTransport()
                       .CurrentTestsConfiguredSqlLayer(connectionStringName: builder.Configuration.Id.ToString());
}
