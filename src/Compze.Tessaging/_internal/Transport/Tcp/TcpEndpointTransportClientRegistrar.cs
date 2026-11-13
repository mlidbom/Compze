using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging._private.Transport;
using Compze.Tessaging._private.Transport.FramedConnections;
using Compze.Tessaging._private.Transport.Tcp;

namespace Compze.Tessaging._internal.Transport.Tcp;

static class TcpEndpointTransportClientRegistrar
{
   ///<summary>Registers the TCP implementation of the endpoint transport's client side (<see cref="IEndpointTransportClient"/>):<br/>
   /// a <see cref="FramedConnectionEndpointTransportClient"/> connecting through a <see cref="TcpConnectionOpener"/>.<br/>
   /// Guarded so that every TCP transport registration can demand it without conflicting when an endpoint speaks several<br/>
   /// communication styles.</summary>
   public static IComponentRegistrar TcpEndpointTransportClientIfNotRegistered(this IComponentRegistrar registrar)
      => registrar.IsRegistered<IEndpointTransportClient>()
            ? registrar
            : registrar.Register(Singleton.For<IEndpointTransportClient>().CreatedBy(() => new FramedConnectionEndpointTransportClient(new TcpConnectionOpener())));
}
