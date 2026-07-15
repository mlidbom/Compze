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
   ///<summary>Registers only the client side of the Typermedia transport, for the current test's <see cref="Transport"/> — for a test client that connects to endpoints without hosting one.<br/>
   /// (An endpoint needs no such registration: the distributed Typermedia feature registers its own client side, on the endpoint transport the protocol declaration supplies.)</summary>
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
