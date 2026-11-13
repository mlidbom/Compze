using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging._internal.Transport;
using Compze.Tessaging._private.Transport.FramedConnections;

namespace Compze.Tessaging._private.Transport.NamedPipes;

static class NamedPipeEndpointTransportServerRegistrar
{
   ///<summary>Registers the named-pipe implementation of the endpoint's one transport server (<see cref="IEndpointTransportServer"/>)<br/>
   /// — a <see cref="FramedConnectionEndpointTransportServer"/> listening through a <see cref="NamedPipeConnectionListener"/> —<br/>
   /// together with the endpoint's <see cref="TransportRequestHandlerMap"/> it serves, unless a transport already registered one:<br/>
   /// guarded so that every communication style's named-pipe transport registration can demand the server without conflicting when<br/>
   /// an endpoint hosts several styles.</summary>
   public static IComponentRegistrar NamedPipeEndpointTransportServerIfNotRegistered(this IComponentRegistrar registrar) =>
      registrar.IsRegistered<IEndpointTransportServer>()
         ? registrar
         : registrar.Register(TransportRequestHandlerMap.RegisterWith)
                    .Register(
                       Singleton.For<IEndpointTransportServer>()
                                .CreatedBy((TransportRequestHandlerMap handlerMap) => new FramedConnectionEndpointTransportServer(new NamedPipeConnectionListener(), handlerMap)));
}
