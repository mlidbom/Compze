using Compze.DependencyInjection.Wiring.Registration;
using Compze.Tessaging.Internals.Transport;

namespace Compze.Tessaging.AspNetCore;

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

   extension<TConcreteBuilder>(Compze.Tessaging.Endpoints.EndpointBuilder<TConcreteBuilder> @this) where TConcreteBuilder : Compze.Tessaging.Endpoints.EndpointBuilder<TConcreteBuilder>
   {
      ///<summary>Declares the endpoint's transport protocol: HTTP served by ASP.NET Core — the network protocol. See<br/>
      /// <see cref="AspNetCoreEndpointTransport(IComponentRegistrar)"/>, to which this delegates.</summary>
      public TConcreteBuilder AspNetCoreEndpointTransport() => @this.TransportProtocol(registrar => registrar.AspNetCoreEndpointTransport());
   }
}
