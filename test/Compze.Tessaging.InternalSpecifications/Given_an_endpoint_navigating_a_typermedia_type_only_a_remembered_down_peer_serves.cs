using Compze.Tessaging.Endpoints;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Must;
using Compze.DependencyInjection;
using Compze.Tessaging.Endpoints.BestEffort;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Peers._internal;
using Compze.Tessaging._internal.Transport.Advertisement;
using Compze.Tessaging.Typermedia;
using Compze.Tessaging.Typermedia.Client;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.TypeIdentifiers;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tessaging.InternalSpecifications;

///<summary>The known-but-down half of exhausted handler-availability patience: when the send throws, the message names the<br/>
/// remembered peer whose last-known advertisement serves the type as known and currently down (the never-seen half lives with<br/>
/// the public-API specifications, see <c>Given_an_endpoint_navigating_a_typermedia_type_with_no_live_route</c>).</summary>
///<remarks>An internal specification because "remembered peer, no live route" is not deterministically producible through the<br/>
/// public API today: a met peer's clean disposal leaves its route until the navigator notices the disconnect, and a navigation<br/>
/// in that notice window dies on the transport connect instead of reaching the waiting-send path. This specification instead<br/>
/// plays the router for a peer that never existed, recording its advertisement directly.</remarks>
public class Given_an_endpoint_navigating_a_typermedia_type_only_a_remembered_down_peer_serves : UniversalTestBase
{
   static readonly EndpointId NavigatorEndpointId = new(Guid.Parse("A7C4E921-8D36-4F15-B2A9-63E80D1C7F54"));

   ///<summary>The remembered peer this specification plays the router for — known-but-down with no process behind it.</summary>
   static readonly EndpointId DownPeerId = new(Guid.Parse("D2E94B71-3C58-4A06-B8F1-7E60A5D49C23"));

   readonly TestingEndpointHost _host;
   readonly BestEffortEndpoint _navigatorEndpoint;
   IRemoteTypermediaNavigator _navigator = null!;

   public Given_an_endpoint_navigating_a_typermedia_type_only_a_remembered_down_peer_serves()
   {
      _host = TestingEndpointHost.Create();
      _navigatorEndpoint = _host.RegisterBestEffortEndpoint(
         "Navigator",
         NavigatorEndpointId,
         endpointBuilder => endpointBuilder
            .RegisterComponents(registrar => registrar.RequireTessagingInternalSpecificationTypeMappings())
            //Short deliberately: this specification pins what exhausted patience says, so waiting out the full default would only slow the suite.
            .HandlerAvailabilityPatience(TimeSpan.FromMilliseconds(100)));
   }

   protected override async Task InitializeAsyncInternal()
   {
      await _host.StartAsync().caf();
      _navigator = _navigatorEndpoint.ServiceLocator.Resolve<IRemoteTypermediaNavigator>();
   }

   protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync().caf();

   [PCT] public async Task the_navigation_fails_after_patience_naming_the_peer_as_known_and_down()
   {
      var tueryTypeIdString = _navigatorEndpoint.ServiceLocator.Resolve<ITypeMap>().GetId(typeof(TueryOnlyADownPeerServes)).CanonicalString;
      await _navigatorEndpoint.ServiceLocator.Resolve<IPeerRegistry>().RecordAdvertisementAsync(new EndpointInformation("DownPeer", DownPeerId, [tueryTypeIdString]));

      (await InvokingAsync(() => _navigator.GetAsync(new TueryOnlyADownPeerServes())).Must().ThrowAsync<NoHandlerForTypermediaTypeException>())
      .Which.Message.Must().Contain(DownPeerId.ToString())
      .Contain("known and currently down");
   }
}
