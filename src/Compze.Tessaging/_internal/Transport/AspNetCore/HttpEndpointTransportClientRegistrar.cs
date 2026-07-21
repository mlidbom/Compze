using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging._private.Transport;
using Compze.Tessaging._private.Transport.AspNetCore;

namespace Compze.Tessaging._internal.Transport.AspNetCore;

static class HttpEndpointTransportClientRegistrar
{
   ///<summary>Registers the HTTP implementation of the endpoint transport's client side (<see cref="IEndpointTransportClient"/>),<br/>
   /// plus the <see cref="IHttpClientFactoryCE"/> it posts through. Guarded so that every HTTP transport registration can demand it<br/>
   /// without conflicting when an endpoint speaks several communication styles.</summary>
   public static IComponentRegistrar HttpEndpointTransportClientIfNotRegistered(this IComponentRegistrar registrar)
      => registrar.IsRegistered<IEndpointTransportClient>()
            ? registrar
            : registrar.HttpClientFactoryCEIfNotRegistered()
                       .Register(Singleton.For<IEndpointTransportClient>()
                                          .CreatedBy((IHttpClientFactoryCE factory) => new HttpEndpointTransportClient(factory)));
}
