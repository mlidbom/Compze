using Compze.Internals.Transport;
using Compze.Typermedia.Client;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE;
using Compze.Internals.Testing;

namespace Compze.Tessaging.Hosting.Testing.Wiring;

static class TestingComponentRegistrarClientTransport
{
   /// <summary>Registers only the client-side transport poster for the current test transport (no inbox server).</summary>
   public static IComponentRegistrar CurrentTestsClientTransport(this IComponentRegistrar register) =>
      register.CastTo<TestingComponentRegistrar>()
              .HttpClientFactoryCE()
              .HttpTypermediaTransport()
              .HttpInfrastructureQueryTransport();
}
