using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Tessaging._private.Transport.AspNetCore;

interface IHttpClientFactoryCE
{
   HttpClient CreateClient();
}

static class HttpClientFactoryCERegistrar
{
   ///<summary>Registers the endpoint's one <see cref="IHttpClientFactoryCE"/>. Guarded so that every HTTP transport registration<br/>
   /// demands it itself — a composing layer never registers it.</summary>
   public static IComponentRegistrar HttpClientFactoryCEIfNotRegistered(this IComponentRegistrar registrar)
      => registrar.IsRegistered<IHttpClientFactoryCE>()
            ? registrar
            : registrar.Register(HttpClientFactoryCE.RegisterWith);
}

class HttpClientFactoryCE : IHttpClientFactoryCE
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(
         Singleton.For<IHttpClientFactoryCE>().CreatedBy(() => new HttpClientFactoryCE()));

   HttpClientFactoryCE() {}

   public HttpClient CreateClient() => new(Handler, disposeHandler: false);

   static readonly SocketsHttpHandler Handler = new()
                                                {
                                                   //todo: Give HTTP the time limits the TCP transport has (Magnus, 2026-11-14: record it, not done now;
                                                   //   HTTP most likely stays as a transport). Today the only limit is HttpClient's default timeout of
                                                   //   100 seconds, with the consequences the bug: comment in HttpEndpointTransportClient describes.
                                                   //   Three changes, which belong together:
                                                   //   - ConnectTimeout of 10 seconds here, the same as FramedConnectionPool's ConnectTimeout.
                                                   //   - A ConnectCallback here that turns TCP keepalive on for each connection, as TcpConnection does:
                                                   //     the operating system then ends a connection to a machine that vanished within about ten
                                                   //     seconds, also while a request awaits its response.
                                                   //   - Timeout.InfiniteTimeSpan as the Timeout of the HttpClient that CreateClient returns: a handler
                                                   //     is then waited for as long as it runs, as over named pipes and TCP.
                                                   //   Verify it by holding the across-machines lab's conversation over HTTP while the lab machine is made
                                                   //   to vanish (C-VM-ConverseWhileTheLabMachineVanishes). The lab cannot do that yet: its program
                                                   //   serves over TCP only, and its machine has no ASP.NET Core runtime, by design. That run also
                                                   //   settles what is so far reasoned and not run: that a request written to a connection whose machine
                                                   //   has already vanished fails after about 10 seconds, as an EndpointUnreachableException, when
                                                   //   Windows gives up resending it.
                                                   PooledConnectionLifetime = TimeSpan.FromMinutes(2)
                                                };
}
