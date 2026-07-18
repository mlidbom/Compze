using Compze.Abstractions.Hosting.Public;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Must;
using Compze.DependencyInjection;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Engine;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Typermedia;
using Compze.Tessaging.Typermedia.Client;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tessaging.Specifications.Typermedia;

///<summary>The ambiguous half of waiting sends, on the endpoint's own navigation path: several live endpoints advertising one<br/>
/// typermedia type is a diagnosable condition the send waits out — a rolling replacement resolves it the moment the retiring<br/>
/// endpoint retracts — and only exhausted patience throws <see cref="MultipleHandlersForTypermediaTypeException"/>, naming the<br/>
/// endpoints, never a silent pick. (The external client's router is pinned separately: its throws stay immediate, because a<br/>
/// client's connections change only by its own explicit connects.)</summary>
public class Given_an_endpoint_navigating_a_typermedia_type_two_live_endpoints_advertise : UniversalTestBase
{
   static readonly EndpointId NavigatorEndpointId = new(Guid.Parse("B4F07D28-5E91-4C36-A0D7-92E8B1C5F6A3"));
   static readonly EndpointId FirstHandlerEndpointId = new(Guid.Parse("1C9E5B72-D840-4F13-86A5-3B0F7D2E9C41"));
   static readonly EndpointId SecondHandlerEndpointId = new(Guid.Parse("E8A31F96-27C4-4D58-B1E0-64D9A0C7B5F2"));

   readonly TestingEndpointHost _host;
   readonly BestEffortEndpoint _navigatorEndpoint;

   public Given_an_endpoint_navigating_a_typermedia_type_two_live_endpoints_advertise()
   {
      _host = TestingEndpointHost.Create();

      _navigatorEndpoint = _host.RegisterBestEffortEndpoint(
         "Navigator",
         NavigatorEndpointId,
         endpointBuilder =>
         {
            endpointBuilder.MapTypes(mapper => mapper.RegisterTypermediaHostingSpecificationTypeMappings());
            //Short deliberately: this specification pins what exhausted patience says, so waiting out the full default would only slow the suite.
            endpointBuilder.HandlerAvailabilityPatience(TimeSpan.FromMilliseconds(100));
         });

      RegisterHandlerEndpoint("FirstHandler", FirstHandlerEndpointId);
      RegisterHandlerEndpoint("SecondHandler", SecondHandlerEndpointId);
   }

   void RegisterHandlerEndpoint(string name, EndpointId id) =>
      _host.RegisterBestEffortEndpoint(
         name,
         id,
         endpointBuilder =>
         {
            endpointBuilder.MapTypes(mapper => mapper.RegisterTypermediaHostingSpecificationTypeMappings());
            endpointBuilder.RegisterTessageHandlers(handle => handle
                       .ForTuery((TueryBothEndpointsHandle _) => new TueryAnswer { Message = $"from {name}" }));
         });

   protected override async Task InitializeAsyncInternal()
   {
      await _host.StartAsync().caf();
      //The ambiguity pin needs BOTH advertising handlers visible to the navigator endpoint before it navigates - with only one
      //discovered, the navigation would succeed instead of failing loud naming both.
      await _host.AwaitEndpointsHaveMetEachOtherAsync().caf();
   }

   protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync().caf();

   [PCT] public async Task the_tuery_fails_after_patience_naming_both_endpoints_and_the_unresolved_ambiguity() =>
      (await InvokingAsync(() => _navigatorEndpoint.ServiceLocator.Resolve<IRemoteTypermediaNavigator>().GetAsync(new TueryBothEndpointsHandle()))
          .Must().ThrowAsync<MultipleHandlersForTypermediaTypeException>())
      .Which.Message.Must().Contain(FirstHandlerEndpointId.ToString())
      .Contain(SecondHandlerEndpointId.ToString())
      .Contain("did not resolve within");
}
