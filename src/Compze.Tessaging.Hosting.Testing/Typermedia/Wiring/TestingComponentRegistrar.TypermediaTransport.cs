using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.SystemCE;
using Compze.Internals.Testing;
using Compze.Tessaging.Transport.AspNet;
using Compze.Tessaging.Transport.NamedPipes;

namespace Compze.Tessaging.Hosting.Testing.Typermedia.Wiring;

using Transport = Compze.Abstractions.Wiring.Testing.Internal.Transport; //Inside the namespace, so it outranks the Compze.Tessaging.Transport namespace this file's new home would otherwise resolve Transport to.

public static class TestingComponentRegistrarTypermediaTransport
{
   ///<summary>Registers the endpoint transport client of the current test's <see cref="Transport"/> — the transport-client<br/>
   /// strategy a pure client's composition declares (<c>TypermediaClientBuilder.TransportProtocol</c>).</summary>
   public static IComponentRegistrar CurrentTestsEndpointTransportClient(this IComponentRegistrar register) =>
      TestEnv.Transport switch
      {
         Transport.AspNetCore => register.CastTo<TestingComponentRegistrar>()
                                         .HttpEndpointTransportClientIfNotRegistered(),
         Transport.NamedPipes => register.CastTo<TestingComponentRegistrar>()
                                         .NamedPipeEndpointTransportClientIfNotRegistered(),
         _ => throw new ArgumentOutOfRangeException()
      };
}
