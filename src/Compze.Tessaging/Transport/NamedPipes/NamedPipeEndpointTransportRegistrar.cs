using Compze.Tessaging.Internal.Transport;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Internal.Transport.Advertisement;

using Compze.Tessaging.Internal.Transport.NamedPipes;
using Compze.Tessaging.Private.Transport.NamedPipes;
using Compze.Tessaging.Private.Transport;

namespace Compze.Tessaging.Transport.NamedPipes;

public static class NamedPipeEndpointTransportRegistrar
{
   ///<summary>Declares the endpoint's transport protocol: named pipes. Registers the named-pipe endpoint transport client<br/>
   /// (<see cref="IEndpointTransportClient"/>), the endpoint-discovery query transport that runs on it, and the endpoint's one<br/>
   /// transport server (<see cref="IEndpointTransportServer"/>) serving every remotable capability's contributed request handlers —<br/>
   /// the same-machine protocol, with no web stack.</summary>
   public static IComponentRegistrar NamedPipeEndpointTransport(this IComponentRegistrar registrar)
      => registrar.NamedPipeEndpointTransportClientIfNotRegistered()
                  .EndpointInformationQueryTransportIfNotRegistered()
                  .NamedPipeEndpointTransportServerIfNotRegistered();

   extension<TConcreteBuilder>(Endpoints.EndpointBuilder<TConcreteBuilder> @this) where TConcreteBuilder : Endpoints.EndpointBuilder<TConcreteBuilder>
   {
      ///<summary>Declares the endpoint's transport protocol: named pipes — the same-machine protocol, with no web stack.<br/>
      /// See <see cref="NamedPipeEndpointTransport(IComponentRegistrar)"/>, to which this delegates.</summary>
      public TConcreteBuilder NamedPipeEndpointTransport() => @this.TransportProtocol(registrar => registrar.NamedPipeEndpointTransport());
   }
}
