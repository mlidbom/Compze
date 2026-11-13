using System.Net;
using Compze.Contracts.Exceptions;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.BestEffort;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.Transport.Tcp;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Hosting;

///<summary>
/// What declaring the TCP transport (<c>TcpEndpointTransport(listenOn)</c>) makes of an endpoint's address: the endpoint
/// listens on the one IP address its declaration names, at a port the operating system picks when it starts, and that
/// address and port are the address it has and announces. An address that stands for every network interface is refused
/// when the endpoint is built: nothing could connect to the address the endpoint would announce.
/// What endpoints then say to each other over TCP is specified by every specification that runs on the Tcp leg of the
/// pluggable-component matrix. The host is the production host and the environment is the specification's own.
///</summary>
public class An_endpoint_declaring_the_TCP_transport : UniversalTestBase
{
   readonly IEndpointHost _host = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder());

   protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync().caf();

   public class to_listen_on_the_loopback_address : An_endpoint_declaring_the_TCP_transport
   {
      readonly BestEffortEndpoint _endpoint;
      readonly BestEffortEndpoint _anotherEndpointDeclaringTheSameAddress;

      public to_listen_on_the_loopback_address()
      {
         var listeningOnTheLoopbackAddress = new EnvironmentDeclaringTheTcpTransport(listenOn: IPAddress.Loopback);
         _endpoint = _host.RegisterEndpoint(new FirstEndpointDeclaration(), listeningOnTheLoopbackAddress);
         _anotherEndpointDeclaringTheSameAddress = _host.RegisterEndpoint(new SecondEndpointDeclaration(), listeningOnTheLoopbackAddress);
      }

      protected override async Task InitializeAsyncInternal() => await _host.StartAsync().caf();

      [PCT] public void once_started_its_address_has_the_scheme_compze_tcp() => _endpoint.Address!.Uri.Scheme.Must().Be("compze.tcp");

      [PCT] public void once_started_its_address_names_the_loopback_address() => _endpoint.Address!.Uri.Host.Must().Be(IPAddress.Loopback.ToString());

      [PCT] public void once_started_its_address_names_a_port_the_operating_system_picked() => _endpoint.Address!.Uri.Port.Must().BeGreaterThan(0);

      [PCT] public void another_endpoint_declaring_the_same_address_listens_at_a_port_of_its_own() =>
         _anotherEndpointDeclaringTheSameAddress.Address!.Uri.Port.Must().NotBe(_endpoint.Address!.Uri.Port);
   }

   public class to_listen_on_an_address_that_stands_for_every_network_interface : An_endpoint_declaring_the_TCP_transport
   {
      [PCT] public void building_it_is_refused_for_the_IPv4_address() =>
         Invoking(() => _host.RegisterEndpoint(new FirstEndpointDeclaration(), new EnvironmentDeclaringTheTcpTransport(listenOn: IPAddress.Any)))
           .Must().Throw<ArgumentAssertionFailedException>()
           .Which.Message.Must().Contain("every network interface");

      [PCT] public void building_it_is_refused_for_the_IPv6_address() =>
         Invoking(() => _host.RegisterEndpoint(new FirstEndpointDeclaration(), new EnvironmentDeclaringTheTcpTransport(listenOn: IPAddress.IPv6Any)))
           .Must().Throw<ArgumentAssertionFailedException>()
           .Which.Message.Must().Contain("every network interface");
   }

   ///<summary>This specification's point, as an environment: the TCP transport listening on the address the specification<br/>
   /// names, whatever transport the current test's combination names, and no place in any topology.</summary>
   class EnvironmentDeclaringTheTcpTransport : IEndpointEnvironment
   {
      readonly IPAddress _listenOn;
      internal EnvironmentDeclaringTheTcpTransport(IPAddress listenOn) => _listenOn = listenOn;

      public void Configure(EndpointBuilder endpointBuilder) => endpointBuilder
         .TcpEndpointTransport(_listenOn)
         .Serializer(registrar => registrar.CurrentTestsSerializersIfNotClonedContainer());

      public void ConfigureDomainDatabase(ExactlyOnceEndpointBuilder endpointBuilder) {} //Never called: the specification's endpoints are best-effort endpoints, which have no domain database.
   }

   class FirstEndpointDeclaration : BestEffortEndpointDeclaration<FirstEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "FirstEndpointDeclaringTheTcpTransport";
      public static EndpointId Id { get; } = new(Guid.Parse("0C1F7B56-3A4E-4D0B-9E0D-6A5B7C1E2F31"));

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireIntegrationTestTypeMappings();
   }

   class SecondEndpointDeclaration : BestEffortEndpointDeclaration<SecondEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "SecondEndpointDeclaringTheTcpTransport";
      public static EndpointId Id { get; } = new(Guid.Parse("7E2D9A14-5B6C-4F3A-8D21-9C0E4B7A6F52"));

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireIntegrationTestTypeMappings();
   }
}
