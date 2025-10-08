using System;
using System.Net.Http;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Hosting.Implementation.Http;

interface IHttpClientFactoryCE
{
   HttpClient CreateClient();
}

class HttpClientFactoryCE : IHttpClientFactoryCE
{
   internal static void RegisterWith(IDependencyRegistrar registrar)
      => registrar.Register(
         Singleton.For<IHttpClientFactoryCE>().CreatedBy(() => new HttpClientFactoryCE()));

   private HttpClientFactoryCE(){}

   public HttpClient CreateClient() => new(Handler, disposeHandler: false);

   static readonly SocketsHttpHandler Handler = new()
                                                {
                                                   PooledConnectionLifetime = TimeSpan.FromMinutes(2)
                                                };
}
