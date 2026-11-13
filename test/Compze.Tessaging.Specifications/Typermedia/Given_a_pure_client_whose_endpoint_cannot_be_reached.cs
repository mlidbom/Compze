using Compze.DependencyInjection.Abstractions;
using Compze.Must;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.BestEffort;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Typermedia;
using Compze.Tessaging.Typermedia;
using Compze.Tessaging.Typermedia.Client;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.xUnitMatrix;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tessaging.Specifications.Typermedia;

using Transport = Compze.Hosting.Testing.Transport; //Inside the namespace, so it outranks the Compze.Tessaging.Transport namespace, which Transport would otherwise resolve to here.

///<summary>What a pure client (<see cref="TypermediaClient"/>) is told when the endpoint it converses with cannot be reached:<br/>
/// an <see cref="EndpointUnreachableException"/>, whatever the transport, both when no connection to the endpoint's address<br/>
/// can be made and when the connection the client has to it has ended.</summary>
public class Given_a_pure_client_whose_endpoint_cannot_be_reached : UniversalTestBase
{
   readonly TestingEndpointHost _host = TestingEndpointHost.Create();
   readonly BestEffortEndpoint _endpoint;

   public Given_a_pure_client_whose_endpoint_cannot_be_reached() => _endpoint = _host.RegisterEndpoint(new GreetingEndpointDeclaration());

   protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync();

   public class because_nothing_listens_at_the_address_it_connects_to : Given_a_pure_client_whose_endpoint_cannot_be_reached
   {
      EndpointAddress _addressWhereNothingListens = null!;

      protected override async Task InitializeAsyncInternal()
      {
         await _host.StartAsync();
         var addressTheEndpointListenedAt = _endpoint.Address!;
         await _endpoint.StopAsync();
         _addressWhereNothingListens = WithAPortNoServerIsEverGiven(addressTheEndpointListenedAt);
      }

      [PCT] public async Task connecting_throws_EndpointUnreachableException() =>
         await InvokingAsync(() => TypermediaTestClient.ConnectTo(_addressWhereNothingListens, registrar => registrar.RequireTypermediaHostingSpecificationTypeMappings()))
              .Must().ThrowAsync<EndpointUnreachableException>();

      ///<summary>The address with its port, if the transport's addresses have one, replaced by a port outside the range the<br/>
      /// operating system picks servers' ports from, so that nothing listens there now or later.</summary>
      ///<remarks>The port the endpoint listened at is free once it has stopped, and the operating system may give it to<br/>
      /// another test's server at any moment. A connect to it would then reach that server: on Windows a refused connect is<br/>
      /// tried again for about a second.</remarks>
      static EndpointAddress WithAPortNoServerIsEverGiven(EndpointAddress address) =>
         address.Uri.Port == -1
            ? address
            : new EndpointAddress(new UriBuilder(address.Uri) { Port = 1 }.Uri);
   }

   public class because_the_endpoint_stopped_after_they_conversed : Given_a_pure_client_whose_endpoint_cannot_be_reached
   {
      TypermediaTestClient _client = null!;

      protected override async Task InitializeAsyncInternal()
      {
         await _host.StartAsync();
         _client = await TypermediaTestClient.ConnectTo(_endpoint.Address!, registrar => registrar.RequireTypermediaHostingSpecificationTypeMappings());
         (await _client.Navigator.GetAsync(new GreetingTuery { Name = "World" })).Message.Must().Be("Hello World!");
         await _endpoint.StopAsync();
      }

      protected override async Task DisposeAsyncInternal()
      {
         await _client.DisposeAsync();
         await base.DisposeAsyncInternal();
      }

      [Skip<Transport>([Transport.AspNetCore, Transport.Tcp], "Over TCP and HTTP the client's next tuery connects anew, to the port the endpoint has left, which the operating system may by then have given to another test's server. A named pipe's address is never anyone else's, so there the tuery meets the connection the stop ended.")]
      [PCT] public async Task its_next_tuery_throws_EndpointUnreachableException() =>
         await InvokingAsync(() => _client.Navigator.GetAsync(new GreetingTuery { Name = "World" }))
              .Must().ThrowAsync<EndpointUnreachableException>();
   }

   class GreetingEndpointDeclaration : BestEffortEndpointDeclaration<GreetingEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "GreetingEndpointThatStops";
      public static EndpointId Id => new(Guid.Parse("b7b1e9a2-6f0c-4d57-9a43-2c8e5d1f7a60"));

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireTypermediaHostingSpecificationTypeMappings();

      protected override void RegisterTueryHandlers(ITueryHandlerRegistrar handle) => handle
         .ForTuery((GreetingTuery tuery) => new Greeting { Message = $"Hello {tuery.Name}!" });
   }
}
