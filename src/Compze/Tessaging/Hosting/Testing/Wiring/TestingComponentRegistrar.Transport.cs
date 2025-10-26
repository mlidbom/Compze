using System;
using Compze.Core.Wiring.Testing.Internal;
using Compze.Tessaging.Hosting.AspNetCore.Wiring;
using Compze.Tessaging.Implementation.Transport.Client.Implementation.Http;
using Compze.Tessaging.Implementation.Transport.Client.Implementation.Memory;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Testing.DbPool;

namespace Compze.Tessaging.Hosting.Testing.Wiring;

public static class TestingComponentRegistrarTransport
{
   public static IComponentRegistrar CurrentTestsTransport(this IComponentRegistrar register) =>
      register.CastTo<TestingComponentRegistrar>().CurrentTestsTransport();

   public static IComponentRegistrar CurrentTestsTransport(this TestingComponentRegistrar @this)
   {
      switch(TestEnv.Transport)
      {
         case Transport.AspNetCore:
            return @this.AspNetCoreTransport()
                        .HttpApiTransportClient();
         case Transport.Memory:
            return @this.MemoryTransport()
                        .MemoryApiTransportClient();
         default:
            throw new ArgumentOutOfRangeException();
      }
   }
}
