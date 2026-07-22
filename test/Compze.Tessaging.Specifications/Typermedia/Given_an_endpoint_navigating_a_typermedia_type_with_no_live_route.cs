using Compze.Tessaging.Endpoints;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Must;
using Compze.DependencyInjection;
using Compze.Tessaging.Endpoints.BestEffort;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Typermedia;
using Compze.Tessaging.Typermedia.Client;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tessaging.Specifications.Typermedia;

///<summary>The patience-exhausted half of waiting sends: the no-handler failure is never thrown immediately — the send first<br/>
/// waits out the endpoint's handler-availability patience — and when it throws, the message names a type nothing this endpoint<br/>
/// has ever met serves as a probable deployment or configuration error (see <c>src/Compze.Tessaging/dev_docs/peer-model.md</c>).<br/>
/// The known-but-down half is an internal specification, <c>Given_an_endpoint_navigating_a_typermedia_type_only_a_remembered_down_peer_serves</c>:<br/>
/// a remembered-but-down peer is not deterministically producible through the public API today.</summary>
public class Given_an_endpoint_navigating_a_typermedia_type_with_no_live_route : UniversalTestBase
{
   readonly TestingEndpointHost _host;
   readonly BestEffortEndpoint _navigatorEndpoint;
   IRemoteTypermediaNavigator _navigator = null!;

   public Given_an_endpoint_navigating_a_typermedia_type_with_no_live_route()
   {
      _host = TestingEndpointHost.Create();
      _navigatorEndpoint = _host.RegisterEndpoint(new NavigatorEndpointDeclaration());
   }

   class NavigatorEndpointDeclaration : BestEffortEndpointDeclaration<NavigatorEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "Navigator";
      public static EndpointId Id => new(Guid.Parse("6F8D2C15-B7A9-4E30-95D6-1A4B8C7E2F90"));

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireTypermediaHostingSpecificationTypeMappings();

      //Short deliberately: these specifications pin what exhausted patience says, so waiting out the full default would only slow the suite.
      protected override TimeSpan? HandlerAvailabilityPatience => TimeSpan.FromMilliseconds(100);
   }

   protected override async Task InitializeAsyncInternal()
   {
      await _host.StartAsync().caf();
      _navigator = _navigatorEndpoint.ServiceLocator.Resolve<IRemoteTypermediaNavigator>();
   }

   protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync().caf();

   [PCT] public async Task navigating_a_type_nothing_it_ever_met_serves_fails_after_patience_naming_the_probable_deployment_error() =>
      (await InvokingAsync(() => _navigator.GetAsync(new TueryNothingServes())).Must().ThrowAsync<NoHandlerForTypermediaTypeException>())
      .Which.Message.Must().Contain(typeof(TueryNothingServes).FullName!)
      .Contain("patience")
      .Contain("Nothing this endpoint has ever met serves the type");
}
