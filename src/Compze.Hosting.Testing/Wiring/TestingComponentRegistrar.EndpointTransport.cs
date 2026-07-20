using Compze.Tessaging.Transport.NamedPipes;
using Compze.Abstractions.Wiring.Testing.Internal;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Testing;
using Compze.Tessaging.Transport.AspNetCore;
using Compze.Tessaging.Internal.Transport.NamedPipes;

namespace Compze.Hosting.Testing.Wiring;

public static class TestingComponentRegistrarEndpointTransport
{
   ///<summary>Declares the endpoint's transport protocol as the current test's <see cref="Transport"/>: the endpoint transport<br/>
   /// client, the endpoint-discovery query transport, and the endpoint's one transport server serving every communication style's<br/>
   /// contributed request handlers.</summary>
   public static IComponentRegistrar CurrentTestsEndpointTransport(this IComponentRegistrar register) =>
      TestEnv.Transport switch
      {
         Transport.AspNetCore => register.AspNetCoreEndpointTransport(),
         Transport.NamedPipes => register.NamedPipeEndpointTransport(),
         _ => throw new ArgumentOutOfRangeException()
      };
}
