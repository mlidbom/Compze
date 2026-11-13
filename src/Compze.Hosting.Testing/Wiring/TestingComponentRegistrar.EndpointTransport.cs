using System.Net;
using Compze.Tessaging.Transport.NamedPipes;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Testing;
using Compze.Tessaging.Transport.AspNetCore;
using Compze.Tessaging.Transport.Tcp;

namespace Compze.Hosting.Testing.Wiring;

public static class TestingComponentRegistrarEndpointTransport
{
   ///<summary>Declares the endpoint's transport protocol as the current test's <see cref="Transport"/>: the endpoint transport<br/>
   /// client, the endpoint-discovery query transport, and the endpoint's one transport server serving every communication style's<br/>
   /// contributed request handlers. The TCP transport listens on the loopback address: a test's endpoints all run on this machine.</summary>
   public static IComponentRegistrar CurrentTestsEndpointTransport(this IComponentRegistrar register) =>
      TestEnv.Transport switch
      {
         Transport.AspNetCore => register.AspNetCoreEndpointTransport(),
         Transport.NamedPipes => register.NamedPipeEndpointTransport(),
         Transport.Tcp => register.TcpEndpointTransport(listenOn: IPAddress.Loopback),
         _ => throw new ArgumentOutOfRangeException()
      };
}
