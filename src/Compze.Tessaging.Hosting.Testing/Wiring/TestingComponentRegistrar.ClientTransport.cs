using Compze.Abstractions.Wiring.Testing.Internal;
using Compze.Internals.Transport;
using Compze.Tessaging.Implementation.Transport.Client.Implementation.Http;
using Compze.Tessaging.Implementation.Transport.Client.Implementation.Memory;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE;
using Compze.Internals.Testing;

namespace Compze.Tessaging.Hosting.Testing.Wiring;

static class TestingComponentRegistrarClientTransport
{
   /// <summary>Registers only the client-side transport poster for the current test transport (no inbox server).</summary>
   public static IComponentRegistrar CurrentTestsClientTransport(this IComponentRegistrar register) =>
      register.CastTo<TestingComponentRegistrar>().CurrentTestsClientTransport();

   static IComponentRegistrar CurrentTestsClientTransport(this TestingComponentRegistrar @this)
   {
      switch(TestEnv.Transport)
      {
         case Transport.AspNetCore:
            return @this.HttpClientFactoryCE()
                        .HttpTypermediaTransport()
                        .HttpInfrastructureQueryTransport();
         case Transport.Memory:
            return @this.MemoryTypermediaTransport()
                        .MemoryInfrastructureQueryTransport();
         default:
            throw new ArgumentOutOfRangeException();
      }
   }
}
