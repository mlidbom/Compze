using Compze.Tessaging._internal.Transport;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging._private.Transport.NamedPipes;
using Compze.Tessaging._private.Transport;

using Compze.Tessaging._internal.Transport.Advertisement;
using Compze.Tessaging.Typermedia.Client;

namespace Compze.Tessaging.Transport.NamedPipes;

public static class NamedPipeEndpointTransportRegistrar
{
   extension(IComponentRegistrar @this)
   {
      ///<summary>Declares the endpoint's transport protocol: named pipes. Registers the named-pipe endpoint transport client<br/>
      /// (<see cref="IEndpointTransportClient"/>), the endpoint-discovery query transport that runs on it, and the endpoint's one<br/>
      /// transport server (<see cref="IEndpointTransportServer"/>) serving every remotable capability's contributed request handlers —<br/>
      /// the same-machine protocol, with no web stack.</summary>
      public IComponentRegistrar NamedPipeEndpointTransport()
         => @this.NamedPipeEndpointTransportClientIfNotRegistered()
                 .EndpointInformationQueryTransportIfNotRegistered()
                 .NamedPipeEndpointTransportServerIfNotRegistered();

      ///<summary>Declares the client side of the named-pipe endpoint transport alone — the transport-client strategy a pure client's<br/>
      /// composition declares (<see cref="TypermediaClientBuilder.ConfigureTransport"/>): a pure client speaks to endpoints, it never serves.</summary>
      public IComponentRegistrar NamedPipeEndpointTransportClient() => @this.NamedPipeEndpointTransportClientIfNotRegistered();
   }

   extension(Endpoints.EndpointBuilder @this)
   {
      ///<summary>Declares the endpoint's transport protocol: named pipes — the same-machine protocol, with no web stack.<br/>
      /// See <see cref="NamedPipeEndpointTransport(IComponentRegistrar)"/>, to which this delegates.</summary>
      public Endpoints.EndpointBuilder NamedPipeEndpointTransport() => @this.TransportProtocol(registrar => registrar.NamedPipeEndpointTransport());
   }
}
