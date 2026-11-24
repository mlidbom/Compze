using Compze.Tessaging._internal.Transport;
using Compze.Tessaging._internal.Transport.AspNetCore;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Typermedia.Client;
using Compze.Tessaging.Transport.AspNetCore._private;
using Compze.Tessaging._private.Transport;

using Compze.Tessaging._internal.Transport.Advertisement;

namespace Compze.Tessaging.Transport.AspNetCore;

public static class AspNetCoreEndpointTransportRegistrar
{
   ///<summary>Declares the endpoint's transport protocol: HTTP served by ASP.NET Core. Registers the HTTP endpoint transport<br/>
   /// client (<see cref="IEndpointTransportClient"/>), the endpoint-discovery query transport that runs on it, and the endpoint's<br/>
   /// one transport server (<see cref="IEndpointTransportServer"/>) serving every remotable capability's contributed request<br/>
   /// handlers.</summary>
   ///<remarks>refactor: The named-pipe transport is declared with an extension of <see cref="Compze.Tessaging.Endpoints.EndpointBuilder"/>,<br/>
   /// <c>endpointBuilder.NamedPipeEndpointTransport()</c>, while this one is only an extension of the registrar, so an environment<br/>
   /// has to write <c>endpointBuilder.TransportProtocol(registrar =&gt; registrar.AspNetCoreEndpointTransport())</c>, which nothing<br/>
   /// points to. An EndpointBuilder extension like the named-pipe one would make the two transports declare alike. (website-and-docs, 2026-11-24)</remarks>
   public static IComponentRegistrar AspNetCoreEndpointTransport(this IComponentRegistrar registrar)
      => registrar.HttpEndpointTransportClientIfNotRegistered()
                  .EndpointInformationQueryTransportIfNotRegistered()
                  .AspNetCoreEndpointTransportServerIfNotRegistered();

   ///<summary>Declares the client side of the HTTP endpoint transport alone — the transport-client strategy a pure client's<br/>
   /// composition declares (<see cref="TypermediaClientBuilder.ConfigureTransport"/>): a pure client speaks to endpoints, it never serves.</summary>
   public static IComponentRegistrar AspNetCoreEndpointTransportClient(this IComponentRegistrar registrar)
      => registrar.HttpEndpointTransportClientIfNotRegistered();
}
