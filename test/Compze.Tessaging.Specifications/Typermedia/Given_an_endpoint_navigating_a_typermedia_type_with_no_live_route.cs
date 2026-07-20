using Compze.Tessaging.Endpoints;
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
/// waits out the endpoint's handler-availability patience — and when it throws, the message tells known-but-down from<br/>
/// never-seen by the peer memory: a remembered peer whose last-known advertisement serves the type is named as known and<br/>
/// currently down; a type nothing this endpoint has ever met serves is named a probable deployment or configuration error<br/>
/// (see <c>src/Compze.Tessaging/dev_docs/peer-model.md</c>).</summary>
public class Given_an_endpoint_navigating_a_typermedia_type_with_no_live_route : UniversalTestBase
{
   static readonly EndpointId NavigatorEndpointId = new(Guid.Parse("6F8D2C15-B7A9-4E30-95D6-1A4B8C7E2F90"));

   ///<summary>A peer that served <see cref="TueryOnlyADownPeerServes"/>, was met, and went down — its endpoint disposed, its<br/>
   /// connection gone, its advertisement still remembered: absence never touches peer memory.</summary>
   static readonly EndpointId DownPeerId = new(Guid.Parse("D2E94B71-3C58-4A06-B8F1-7E60A5D49C23"));

   readonly TestingEndpointHost _host;
   readonly BestEffortEndpoint _navigatorEndpoint;
   IRemoteTypermediaNavigator _navigator = null!;

   public Given_an_endpoint_navigating_a_typermedia_type_with_no_live_route()
   {
      _host = TestingEndpointHost.Create();
      _navigatorEndpoint = _host.RegisterBestEffortEndpoint(
         "Navigator",
         NavigatorEndpointId,
         endpointBuilder => endpointBuilder
            .RegisterComponents(registrar => registrar.RequireTypermediaHostingSpecificationTypeMappings())
            //Short deliberately: these specifications pin what exhausted patience says, so waiting out the full default would only slow the suite.
            .HandlerAvailabilityPatience(TimeSpan.FromMilliseconds(100)));
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

   [PCT] public async Task navigating_a_type_only_a_remembered_down_peer_serves_fails_after_patience_naming_the_peer_as_known_and_down()
   {
      //The navigator meets the peer serving the type, and the peer then goes down: its endpoint disposed, its connection
      //gone, its advertisement still remembered - absence never touches peer memory.
      var downPeerEndpoint = _host.RegisterBestEffortEndpoint(
         "DownPeer",
         DownPeerId,
         endpointBuilder => endpointBuilder
            .RegisterComponents(registrar => registrar.RequireTypermediaHostingSpecificationTypeMappings())
            .RegisterTypermediaHandlers(handle => handle.ForTuery((TueryOnlyADownPeerServes _) => new TueryAnswer())));
      await downPeerEndpoint.StartAsync();
      await _host.AwaitEndpointsHaveMetEachOtherAsync();
      await downPeerEndpoint.DisposeAsync();

      (await InvokingAsync(() => _navigator.GetAsync(new TueryOnlyADownPeerServes())).Must().ThrowAsync<NoHandlerForTypermediaTypeException>())
      .Which.Message.Must().Contain(DownPeerId.ToString())
      .Contain("known and currently down");
   }
}
