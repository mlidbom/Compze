using Compze.Abstractions.Hosting.Public;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Internals.Transport;

namespace Compze.Tessaging.AspNetCore;

public static class AspNetCoreEndpointTransportRegistrar
{
   ///<summary>Declares the endpoint's transport protocol: HTTP served by ASP.NET Core. Registers the HTTP endpoint transport<br/>
   /// client (<see cref="IEndpointTransportClient"/>), the endpoint-discovery query transport that runs on it, and the endpoint's<br/>
   /// one transport server (<see cref="IEndpointTransportServer"/>) serving every communication style's contributed request<br/>
   /// handlers. The communication styles themselves register nothing protocol-specific.</summary>
   public static IComponentRegistrar AspNetCoreEndpointTransport(this IComponentRegistrar registrar)
      => registrar.HttpEndpointTransportClientIfNotRegistered()
                  .EndpointDiscoveryQueryTransportIfNotRegistered()
                  .AspNetCoreEndpointTransportServerIfNotRegistered();

   extension(EndpointComposer @this)
   {
      ///<summary>Declares the endpoint's transport protocol: HTTP served by ASP.NET Core — see<br/>
      /// <see cref="AspNetCoreEndpointTransport(IComponentRegistrar)"/>, to which this delegates. Returns the endpoint's foundation<br/>
      /// (<see cref="EndpointFoundation"/>), on which the database is declared and the distributed features are added.</summary>
      public EndpointFoundation AspNetCoreEndpointTransport()
      {
         @this.Builder.Registrar.AspNetCoreEndpointTransport();
         return new EndpointFoundation(@this.Builder);
      }
   }

}

//Its own class only because the two extension blocks' generated names would otherwise collide (CA1708); folds into
//AspNetCoreEndpointTransportRegistrar when the EndpointComposer surface dies with the feature machinery.
public static class AspNetCoreEndpointTransportDeclaration
{
   extension(Compze.Tessaging.Endpoints.EndpointBuilder @this)
   {
      ///<summary>Declares the endpoint's transport protocol: HTTP served by ASP.NET Core — the network protocol. See<br/>
      /// <see cref="AspNetCoreEndpointTransportRegistrar.AspNetCoreEndpointTransport(IComponentRegistrar)"/>, to which this delegates.</summary>
      public void AspNetCoreEndpointTransport() => @this.TransportProtocol(registrar => registrar.AspNetCoreEndpointTransport());
   }
}
