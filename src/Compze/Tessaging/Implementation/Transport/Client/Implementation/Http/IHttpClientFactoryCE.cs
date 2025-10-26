using System;
using System.Net.Http;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation.Http;

interface IHttpClientFactoryCE
{
   HttpClient CreateClient();
}

static class HttpClientFactoryCERegistrar
{
   internal static IComponentRegistrar HttpClientFactoryCE(this IComponentRegistrar registrar)
      => registrar.Register(Http.HttpClientFactoryCE.RegisterWith);
}

class HttpClientFactoryCE : IHttpClientFactoryCE
{
   internal static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(
         Singleton.For<IHttpClientFactoryCE>().CreatedBy(() => new HttpClientFactoryCE()));

   private HttpClientFactoryCE() {}

   public HttpClient CreateClient() => new(Handler, disposeHandler: false);

   static readonly SocketsHttpHandler Handler = new()
                                                {
                                                   PooledConnectionLifetime = TimeSpan.FromMinutes(2)
                                                };
}
