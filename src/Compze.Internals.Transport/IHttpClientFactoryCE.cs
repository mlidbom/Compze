using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Transport;

public interface IHttpClientFactoryCE
{
   HttpClient CreateClient();
}

public static class HttpClientFactoryCERegistrar
{
   ///<summary>Registers the endpoint's one <see cref="IHttpClientFactoryCE"/>. Guarded so that every HTTP transport registration<br/>
   /// demands it itself — a composing layer never registers it.</summary>
   public static IComponentRegistrar HttpClientFactoryCEIfNotRegistered(this IComponentRegistrar registrar)
      => registrar.IsRegistered<IHttpClientFactoryCE>()
            ? registrar
            : registrar.Register(Transport.HttpClientFactoryCE.RegisterWith);
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
                                                   PooledConnectionLifetime = TimeSpan.FromMinutes(2)
                                                };
}
