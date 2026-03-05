using Compze.Core.Wiring.Testing.Internal;
using Compze.Tessaging.Hosting.AspNetCore.Wiring;
using Compze.Tessaging.Implementation.Transport.Client.Implementation.Memory;
using Compze.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;

namespace Compze.Tessaging.Hosting.Testing.Wiring;

static class TestingComponentRegistrarTransport
{
   public static IComponentRegistrar CurrentTestsTransport(this IComponentRegistrar register) =>
      register.CastTo<TestingComponentRegistrar>().CurrentTestsTransport();

   static IComponentRegistrar CurrentTestsTransport(this TestingComponentRegistrar @this)
   {
      switch(TestEnv.Transport)
      {
         case Transport.AspNetCore:
            return @this.AspNetCoreTransport();
         case Transport.Memory:
            return @this.MemoryTransport()
                        .MemoryApiTransportClient();
         default:
            throw new ArgumentOutOfRangeException();
      }
   }
}
