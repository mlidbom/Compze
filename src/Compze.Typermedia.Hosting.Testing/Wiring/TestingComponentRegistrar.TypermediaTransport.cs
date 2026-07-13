using Compze.Abstractions.Wiring.Testing.Internal;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.SystemCE;
using Compze.Internals.Testing;
using Compze.Internals.Transport;
using Compze.Internals.Transport.NamedPipes;
using Compze.Typermedia.Client;
using Compze.Typermedia.Hosting.AspNetCore.Wiring;

namespace Compze.Typermedia.Hosting.Testing.Wiring;

public static class TestingComponentRegistrarTypermediaTransport
{
   ///<summary>Registers the current test's <see cref="Transport"/> implementation of the Typermedia transport for an endpoint — the transport server plus the client transport — and the shared infrastructure transport if nothing else registered it yet.</summary>
   public static IComponentRegistrar CurrentTestsTypermediaTransport(this IComponentRegistrar register) =>
      TestEnv.Transport switch
      {
         Transport.AspNetCore => register.CurrentTestsInfrastructureTransportIfNotRegistered()
                                         .HttpTypermediaTransport()
                                         .AspNetCoreTypermediaTransportServer(),
         Transport.NamedPipes => register.CurrentTestsInfrastructureTransportIfNotRegistered()
                                         .NamedPipeTypermediaTransport()
                                         .NamedPipeTypermediaTransportServer(),
         _ => throw new ArgumentOutOfRangeException()
      };

   ///<summary>Registers only the client side of the Typermedia transport, for the current test's <see cref="Transport"/> — for a test client that connects to endpoints without hosting one.</summary>
   public static IComponentRegistrar CurrentTestsTypermediaClientTransport(this IComponentRegistrar register) =>
      TestEnv.Transport switch
      {
         Transport.AspNetCore => register.CastTo<TestingComponentRegistrar>()
                                         .HttpClientFactoryCE()
                                         .HttpTypermediaTransport()
                                         .HttpInfrastructureQueryTransport(),
         Transport.NamedPipes => register.CastTo<TestingComponentRegistrar>()
                                         .NamedPipeTypermediaTransport()
                                         .NamedPipeInfrastructureQueryTransport(),
         _ => throw new ArgumentOutOfRangeException()
      };
}
