using Compze.Tessaging.Endpoints;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Must;
using Compze.DependencyInjection;
using Compze.Tessaging.Endpoints.BestEffort;
using Compze.Tessaging.Engine.HandlerRegistration.TessageHandlers;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Typermedia;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tessaging.Specifications.Typermedia;

///<summary>The core waiting-send pin: a remote typermedia send whose type has no live route does not explode — it waits,<br/>
/// within the endpoint's handler-availability patience, for the route to appear, then proceeds normally. Pinned by firing the<br/>
/// navigation before the serving endpoint exists at all: the send is already waiting when the serving endpoint is composed,<br/>
/// announced, and first met (see <c>src/Compze.Tessaging/dev_docs/peer-model.md</c>).</summary>
public class Given_an_endpoint_whose_typermedia_navigation_races_discovery : UniversalTestBase
{
   static readonly EndpointId NavigatorEndpointId = new(Guid.Parse("3E1B7A64-92D4-4B08-8E15-6C0A9F27D3B1"));
   static readonly EndpointId LateEndpointId = new(Guid.Parse("A7C25E90-14FB-4D67-B3A8-0D5E96C1F482"));

   readonly TestingEndpointHost _host;
   readonly BestEffortEndpoint _navigatorEndpoint;

   public Given_an_endpoint_whose_typermedia_navigation_races_discovery()
   {
      _host = TestingEndpointHost.Create();
      _navigatorEndpoint = _host.RegisterBestEffortEndpoint(
         "Navigator",
         NavigatorEndpointId,
         endpointBuilder => endpointBuilder.RegisterComponents(registrar => registrar.RequireTypermediaHostingSpecificationTypeMappings()));
   }

   protected override async Task InitializeAsyncInternal() => await _host.StartAsync().caf();

   protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync().caf();

   [PCT] public async Task a_tuery_sent_before_the_serving_endpoint_exists_waits_and_succeeds_on_its_first_contact()
   {
      //The navigation begins now, with no live route anywhere: the serving endpoint is not merely down, it has never existed.
      var tueryTask = _navigatorEndpoint.ServiceLocator.Resolve<IRemoteTypermediaNavigator>().GetAsync(new TueryServedByTheLateEndpoint());

      var lateEndpoint = _host.RegisterBestEffortEndpoint(
         "LateHandler",
         LateEndpointId,
         endpointBuilder => endpointBuilder
            .RegisterComponents(registrar => registrar.RequireTypermediaHostingSpecificationTypeMappings())
            .RegisterTessageHandlers(handle => handle
                       .ForTuery((TueryServedByTheLateEndpoint _) => new TueryAnswer { Message = "served on first contact" })));
      //The late endpoint starts now, driving its own phase ordering. Its announcement is what the waiting send's patience is
      //spent waiting for.
      await lateEndpoint.StartAsync();

      (await tueryTask).Message.Must().Be("served on first contact");
   }
}
