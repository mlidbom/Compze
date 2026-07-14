using Compze.Abstractions.Wiring.Testing.Internal;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.SystemCE;
using Compze.Internals.Testing;
using Compze.Internals.Transport;
using Compze.Internals.Transport.NamedPipes;
using Compze.Typermedia.Client;

namespace Compze.Typermedia.Hosting.Testing.Wiring;

public static class TestingComponentRegistrarTypermediaTransport
{
   ///<summary>Registers the Typermedia transport client for an endpoint, plus the endpoint transport of the current test's<br/>
   /// <see cref="Transport"/> it runs on. The server-side request handling is registered by the distributed Typermedia feature itself.</summary>
   public static IComponentRegistrar CurrentTestsTypermediaTransport(this IComponentRegistrar register) =>
      register.CurrentTestsEndpointTransport()
              .TypermediaTransport();

   ///<summary>Registers only the client side of the Typermedia transport, for the current test's <see cref="Transport"/> — for a test client that connects to endpoints without hosting one.</summary>
   public static IComponentRegistrar CurrentTestsTypermediaClientTransport(this IComponentRegistrar register) =>
      TestEnv.Transport switch
      {
         Transport.AspNetCore => register.CastTo<TestingComponentRegistrar>()
                                         .HttpEndpointTransportClientIfNotRegistered()
                                         .TypermediaTransport(),
         Transport.NamedPipes => register.CastTo<TestingComponentRegistrar>()
                                         .NamedPipeEndpointTransportClientIfNotRegistered()
                                         .TypermediaTransport(),
         _ => throw new ArgumentOutOfRangeException()
      };
}
