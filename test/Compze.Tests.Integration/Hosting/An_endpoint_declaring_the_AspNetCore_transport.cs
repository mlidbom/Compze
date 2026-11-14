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
using Compze.Tessaging.Transport.AspNetCore;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Hosting;

///<summary>
/// What declaring the AspNetCore transport (<c>AspNetCoreEndpointTransport()</c>) on an endpoint's <see cref="EndpointBuilder"/>
/// makes of the endpoint: once started, it listens for HTTP, and its address is an http address.
/// What endpoints then say to each other over HTTP is specified by every specification that runs on the AspNetCore leg of
/// the pluggable-component matrix. The host is the production host and the environment is the specification's own.
///</summary>
public class An_endpoint_declaring_the_AspNetCore_transport : UniversalTestBase
{
   readonly IEndpointHost _host = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder());
   readonly BestEffortEndpoint _endpoint;

   public An_endpoint_declaring_the_AspNetCore_transport() =>
      _endpoint = _host.RegisterEndpoint(new EndpointDeclaration(), new EnvironmentDeclaringTheAspNetCoreTransport());

   protected override async Task InitializeAsyncInternal() => await _host.StartAsync().caf();
   protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync().caf();

   [PCT] public void once_started_its_address_has_the_scheme_http() => _endpoint.Address!.Uri.Scheme.Must().Be("http");

   ///<summary>This specification's point, as an environment: the AspNetCore transport, whatever transport the current<br/>
   /// test's combination names, and no place in any topology.</summary>
   class EnvironmentDeclaringTheAspNetCoreTransport : IEndpointEnvironment
   {
      public void Configure(EndpointBuilder endpointBuilder) => endpointBuilder
         .AspNetCoreEndpointTransport()
         .Serializer(registrar => registrar.CurrentTestsSerializersIfNotClonedContainer());

      public void ConfigureDomainDatabase(ExactlyOnceEndpointBuilder endpointBuilder) {} //Never called: the specification's endpoint is a best-effort endpoint, which has no domain database.
   }

   class EndpointDeclaration : BestEffortEndpointDeclaration<EndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "EndpointDeclaringTheAspNetCoreTransport";
      public static EndpointId Id { get; } = new(Guid.Parse("5D0B6E8A-2F47-4C19-B3A6-8E1C7F4D9A20"));

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireIntegrationTestTypeMappings();
   }
}
