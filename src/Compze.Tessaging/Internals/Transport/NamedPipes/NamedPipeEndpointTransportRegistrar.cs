using Compze.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Internals.Transport.NamedPipes;

public static class NamedPipeEndpointTransportRegistrar
{
   ///<summary>Declares the endpoint's transport protocol: named pipes. Registers the named-pipe endpoint transport client<br/>
   /// (<see cref="IEndpointTransportClient"/>), the endpoint-discovery query transport that runs on it, and the endpoint's one<br/>
   /// transport server (<see cref="IEndpointTransportServer"/>) serving every remotable capability's contributed request handlers —<br/>
   /// the same-machine protocol, with no web stack.</summary>
   public static IComponentRegistrar NamedPipeEndpointTransport(this IComponentRegistrar registrar)
      => registrar.NamedPipeEndpointTransportClientIfNotRegistered()
                  .EndpointDiscoveryQueryTransportIfNotRegistered()
                  .NamedPipeEndpointTransportServerIfNotRegistered();

   extension(Endpoints.EndpointBuilder @this)
   {
      ///<summary>Declares the endpoint's transport protocol: named pipes — the same-machine protocol, with no web stack.<br/>
      /// See <see cref="NamedPipeEndpointTransport(IComponentRegistrar)"/>, to which this delegates.</summary>
      public void NamedPipeEndpointTransport() => @this.TransportProtocol(registrar => registrar.NamedPipeEndpointTransport());
   }
}
