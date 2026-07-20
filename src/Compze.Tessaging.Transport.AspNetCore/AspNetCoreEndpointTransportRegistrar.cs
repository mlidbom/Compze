using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Transport.Discovery;

namespace Compze.Tessaging.Transport.AspNetCore;

public static class AspNetCoreEndpointTransportRegistrar
{
   ///<summary>Declares the endpoint's transport protocol: HTTP served by ASP.NET Core. Registers the HTTP endpoint transport<br/>
   /// client (<see cref="IEndpointTransportClient"/>), the endpoint-discovery query transport that runs on it, and the endpoint's<br/>
   /// one transport server (<see cref="IEndpointTransportServer"/>) serving every remotable capability's contributed request<br/>
   /// handlers.</summary>
   public static IComponentRegistrar AspNetCoreEndpointTransport(this IComponentRegistrar registrar)
      => registrar.HttpEndpointTransportClientIfNotRegistered()
                  .EndpointDiscoveryQueryTransportIfNotRegistered()
                  .AspNetCoreEndpointTransportServerIfNotRegistered();
}
