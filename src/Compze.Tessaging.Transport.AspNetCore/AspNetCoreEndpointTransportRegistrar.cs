using Compze.Tessaging.Internal.Transport;
using Compze.Tessaging.Internal.Transport.AspNetCore;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Internal.Transport.Advertisement;
using Compze.Tessaging.Typermedia.Client;
using Compze.Tessaging.Transport.AspNetCore.Private;
using Compze.Tessaging.Private.Transport;

namespace Compze.Tessaging.Transport.AspNetCore;

public static class AspNetCoreEndpointTransportRegistrar
{
   ///<summary>Declares the endpoint's transport protocol: HTTP served by ASP.NET Core. Registers the HTTP endpoint transport<br/>
   /// client (<see cref="IEndpointTransportClient"/>), the endpoint-discovery query transport that runs on it, and the endpoint's<br/>
   /// one transport server (<see cref="IEndpointTransportServer"/>) serving every remotable capability's contributed request<br/>
   /// handlers.</summary>
   public static IComponentRegistrar AspNetCoreEndpointTransport(this IComponentRegistrar registrar)
      => registrar.HttpEndpointTransportClientIfNotRegistered()
                  .EndpointInformationQueryTransportIfNotRegistered()
                  .AspNetCoreEndpointTransportServerIfNotRegistered();

   ///<summary>Declares the client side of the HTTP endpoint transport alone — the transport-client strategy a pure client's<br/>
   /// composition declares (<see cref="TypermediaClientBuilder.ConfigureTransport"/>): a pure client speaks to endpoints, it never serves.</summary>
   public static IComponentRegistrar AspNetCoreEndpointTransportClient(this IComponentRegistrar registrar)
      => registrar.HttpEndpointTransportClientIfNotRegistered();
}
