using System;
using Compze.Core.Wiring.Testing.Internal;
using Compze.Tessaging.Hosting.AspNetCore.Wiring;
using Compze.Tessaging.Implementation.Transport.Client.Implementation.Http;
using Compze.Tessaging.Implementation.Transport.Client.Implementation.Memory;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;

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
                        .HttpApiTransportClient();
         case Transport.Memory:
            return @this.MemoryApiTransportClient();
         default:
            throw new ArgumentOutOfRangeException();
      }
   }
}
