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
using Compze.Tessaging.Typermedia;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tessaging.Specifications.Typermedia;
///<summary>The end-to-end proof that a typermedia-only composition works: a best-effort endpoint whose roster holds only typermedia handlers serves a remote client.</summary>
public class Given_an_endpoint_hosting_only_typermedia : UniversalTestBase
{
   readonly TestingEndpointHost _host;
   readonly BestEffortEndpoint _endpoint;
   TypermediaTestClient _client = null!;

   public Given_an_endpoint_hosting_only_typermedia()
   {
      _host = TestingEndpointHost.Create();

      _endpoint = _host.RegisterBestEffortEndpoint(
         "TypermediaOnly",
         new EndpointId(Guid.Parse("4A0EFCC3-49B6-4B8F-8F90-2E12B4B3A1D2")),
         endpointBuilder => endpointBuilder
            .MapTypes(mapper => mapper.RegisterTypermediaHostingSpecificationTypeMappings())
            .RegisterTessageHandlers(handle => handle
                       .ForTuery((GreetingTuery tuery) => new Greeting { Message = $"Hello {tuery.Name}!" })
                       .ForTommand((RegisterGreeterTommand tommand) => new GreeterRegistered { Name = tommand.Name })));
   }

   protected override async Task InitializeAsyncInternal()
   {
      await _host.StartAsync().caf();
      _client = await TypermediaTestClient.ConnectTo(_endpoint.Address!, mapper => mapper.RegisterTypermediaHostingSpecificationTypeMappings()).caf();
   }

   protected override async Task DisposeAsyncInternal()
   {
      await _client.DisposeAsync().caf();
      await _host.DisposeAsync().caf();
   }

   [PCT] public void a_remote_client_executing_a_tuery_receives_the_handlers_result() =>
      _client.Navigator.Get(new GreetingTuery { Name = "World" }).Message.Must().Be("Hello World!");

   [PCT] public void a_remote_client_posting_a_tommand_receives_the_handlers_result() =>
      _client.Navigator.Post(RegisterGreeterTommand.Create("Greta")).Name.Must().Be("Greta");

   [PCT] public void a_remote_client_can_navigate_a_chain_of_steps() =>
      _client.Navigator.Navigate(NavigationSpecification.Get(new GreetingTuery { Name = "World" })
                                                        .Select(greeting => greeting.Message))
             .Must().Be("Hello World!");
}
