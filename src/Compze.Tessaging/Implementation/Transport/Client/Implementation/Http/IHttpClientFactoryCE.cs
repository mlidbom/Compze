using System;
using System.Net.Http;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation.Http;

public interface IHttpClientFactoryCE
{
   HttpClient CreateClient();
}

public static class HttpClientFactoryCERegistrar
{
   public static IComponentRegistrar HttpClientFactoryCE(this IComponentRegistrar registrar)
      => registrar.Register(Http.HttpClientFactoryCE.RegisterWith);
}

public class HttpClientFactoryCE : IHttpClientFactoryCE
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(
         Singleton.For<IHttpClientFactoryCE>().CreatedBy(() => new HttpClientFactoryCE()));

   HttpClientFactoryCE() {}

   public HttpClient CreateClient() => new(Handler, disposeHandler: false);

   static readonly SocketsHttpHandler Handler = new()
                                                {
                                                   PooledConnectionLifetime = TimeSpan.FromMinutes(2)
                                                };
}
