using Compze.Abstractions.Hosting.Public;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Must;

using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Typermedia.Client;
using Compze.Tessaging.Engine;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Typermedia;
using static Compze.Must.MustActions;
using Compze.Tessaging.Typermedia;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tessaging.Specifications.Typermedia;
///<summary>A typermedia tessage executes on exactly one handler, so several live endpoints advertising the same type is a<br/>
/// diagnosable send-time condition — <see cref="MultipleHandlersForTypermediaTypeException"/> naming the endpoints — never a<br/>
/// route-table rebuild failure and never a silent pick. The second specification pins the never-a-rebuild-failure half: routes<br/>
/// registered by the same rebuild that saw the duplicate must work.</summary>
public class Given_two_endpoints_advertising_the_same_typermedia_tuery_type : UniversalTestBase
{
   static readonly EndpointId FirstEndpointId = new(Guid.Parse("07500559-DE01-4EBA-A1C5-71B0FFBAA0C0"));
   static readonly EndpointId SecondEndpointId = new(Guid.Parse("D5A0272D-CE21-476F-8ADE-B0AF5FB1DC01"));

   readonly TestingEndpointHost _host;
   readonly BestEffortEndpoint _firstEndpoint;
   readonly BestEffortEndpoint _secondEndpoint;
   TypermediaTestClient _client = null!;

   public Given_two_endpoints_advertising_the_same_typermedia_tuery_type()
   {
      _host = TestingEndpointHost.Create();

      _firstEndpoint = _host.RegisterBestEffortEndpoint(
         "FirstHandler",
         FirstEndpointId,
         endpointBuilder =>
         {
            endpointBuilder.MapTypes(mapper => mapper.RegisterTypermediaHostingSpecificationTypeMappings());
            endpointBuilder.RegisterTessageHandlers(handle => handle
                       .ForTuery((TueryBothEndpointsHandle _) => new TueryAnswer { Message = "from the first endpoint" }));
         });

      _secondEndpoint = _host.RegisterBestEffortEndpoint(
         "SecondHandler",
         SecondEndpointId,
         endpointBuilder =>
         {
            endpointBuilder.MapTypes(mapper => mapper.RegisterTypermediaHostingSpecificationTypeMappings());
            endpointBuilder.RegisterTessageHandlers(handle => handle
                       .ForTuery((TueryBothEndpointsHandle _) => new TueryAnswer { Message = "from the second endpoint" })
                       .ForTuery((TueryOnlyTheSecondEndpointHandles _) => new TueryAnswer { Message = "only the second endpoint handles this" }));
         });
   }

   protected override async Task InitializeAsyncInternal()
   {
      await _host.StartAsync().caf();
      _client = await TypermediaTestClient.ConnectTo(_firstEndpoint.Address!, mapper => mapper.RegisterTypermediaHostingSpecificationTypeMappings()).caf();
      await _client.AlsoConnectTo(_secondEndpoint.Address!).caf();
   }

   protected override async Task DisposeAsyncInternal()
   {
      await _client.DisposeAsync().caf();
      await _host.DisposeAsync().caf();
   }

   [PCT] public void executing_the_tuery_both_advertise_fails_loud_naming_both_endpoints() =>
      Invoking(() => _client.Navigator.Get(new TueryBothEndpointsHandle()))
         .Must().Throw<MultipleHandlersForTypermediaTypeException>()
         .Which.Message.Must().Contain(FirstEndpointId.ToString())
         .Contain(SecondEndpointId.ToString());

   [PCT] public void a_tuery_only_the_second_endpoint_advertises_still_routes_to_it() =>
      _client.Navigator.Get(new TueryOnlyTheSecondEndpointHandles()).Message.Must().Be("only the second endpoint handles this");
}
