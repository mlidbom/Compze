using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Internals.Testing;
using Compze.Tessaging.Transport.AspNetCore;
using Compze.Tessaging.Transport.NamedPipes;
using Compze.Tessaging.Transport.Tcp;
using Compze.Tessaging.Typermedia.Client;

namespace Compze.Tessaging.Hosting.Testing.Typermedia.Wiring;

using Transport = Compze.Hosting.Testing.Transport; //Inside the namespace, so it outranks the Compze.Tessaging.Transport namespace, which Transport would otherwise resolve to here.

public static class TestingComponentRegistrarTypermediaTransport
{
   ///<summary>Registers the endpoint transport client of the current test's <see cref="Transport"/> — the transport-client<br/>
   /// strategy a pure client's composition declares (<see cref="TypermediaClientBuilder.ConfigureTransport"/>), through the<br/>
   /// registration each transport offers an application's pure client.</summary>
   public static IComponentRegistrar CurrentTestsEndpointTransportClient(this IComponentRegistrar register) =>
      TestEnv.Transport switch
      {
         Transport.AspNetCore => register.AspNetCoreEndpointTransportClient(),
         Transport.NamedPipes => register.NamedPipeEndpointTransportClient(),
         Transport.Tcp        => register.TcpEndpointTransportClient(),
         _                    => throw new ArgumentOutOfRangeException()
      };
}
