using Compze.Abstractions.Wiring.Testing.Internal;
using Compze.Core.Tessaging.Transport.Internal;
using Compze.Tessaging.Hosting.AspNetCore.Wiring;
using Compze.Tessaging.Implementation.Transport.Client.Implementation.Memory;
using Compze.Typermedia.Client;
using Compze.Internals.Transport;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE;
using Compze.Internals.Testing;

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
                        .MemoryApiTransportClient()
                        .MemoryTypermediaTransport()
                        .MemoryInfrastructureQueryTransport()
                        .MemoryTypermediaTransportServer()
                        .MemoryInfrastructureTransportServer()
                        .MemorySupplementalTransportServers();
         default:
            throw new ArgumentOutOfRangeException();
      }
   }

   static IComponentRegistrar MemorySupplementalTransportServers(this IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<IReadOnlyList<ISupplementalTransportServer>>()
                  .CreatedBy((MemoryTypermediaTransportServer typermedia, MemoryInfrastructureTransportServer infrastructure)
                                => (IReadOnlyList<ISupplementalTransportServer>)[typermedia, infrastructure]));
}
