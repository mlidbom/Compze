using System.Net;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging._internal.Transport;
using Compze.Tessaging._private.Transport.FramedConnections;

namespace Compze.Tessaging._private.Transport.Tcp;

static class TcpEndpointTransportServerRegistrar
{
   ///<summary>Registers the TCP implementation of the endpoint's one transport server (<see cref="IEndpointTransportServer"/>)<br/>
   /// — a <see cref="FramedConnectionEndpointTransportServer"/> listening through a <see cref="TcpConnectionListener"/> on<br/>
   /// <paramref name="listenOn"/> — together with the endpoint's <see cref="TransportRequestHandlerMap"/> it serves, unless a<br/>
   /// transport already registered one: guarded so that every communication style's TCP transport registration can demand the<br/>
   /// server without conflicting when an endpoint hosts several styles.</summary>
   public static IComponentRegistrar TcpEndpointTransportServerIfNotRegistered(this IComponentRegistrar registrar, IPAddress listenOn) =>
      registrar.IsRegistered<IEndpointTransportServer>()
         ? registrar
         : registrar.Register(TransportRequestHandlerMap.RegisterWith)
                    .Register(
                       Singleton.For<IEndpointTransportServer>()
                                .CreatedBy((TransportRequestHandlerMap handlerMap) => new FramedConnectionEndpointTransportServer(new TcpConnectionListener(listenOn), handlerMap)));
}
