using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.SystemCE;
using Compze.Internals.Transport;
using Compze.Typermedia.Client;
using Compze.Typermedia.Hosting.AspNetCore.Wiring;

namespace Compze.Typermedia.Hosting.Testing.Wiring;

public static class TestingComponentRegistrarTypermediaTransport
{
   ///<summary>Registers the Typermedia transport for an endpoint — the ASP.NET Core transport server plus the HTTP client transport — and the shared infrastructure transport if nothing else registered it yet.</summary>
   public static IComponentRegistrar CurrentTestsTypermediaTransport(this IComponentRegistrar register) =>
      register.CurrentTestsInfrastructureTransportIfNotRegistered()
              .HttpTypermediaTransport()
              .AspNetCoreTypermediaTransportServer();

   ///<summary>Registers only the client side of the Typermedia transport — for a test client that connects to endpoints without hosting one.</summary>
   public static IComponentRegistrar CurrentTestsTypermediaClientTransport(this IComponentRegistrar register) =>
      register.CastTo<TestingComponentRegistrar>()
              .HttpClientFactoryCE()
              .HttpTypermediaTransport()
              .HttpInfrastructureQueryTransport();
}
