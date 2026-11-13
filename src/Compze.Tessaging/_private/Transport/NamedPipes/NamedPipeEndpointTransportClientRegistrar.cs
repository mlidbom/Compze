using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging._private.Transport;
using Compze.Tessaging._private.Transport.FramedConnections;

namespace Compze.Tessaging._private.Transport.NamedPipes;

static class NamedPipeEndpointTransportClientRegistrar
{
   ///<summary>Registers the named-pipe implementation of the endpoint transport's client side (<see cref="IEndpointTransportClient"/>):<br/>
   /// a <see cref="FramedConnectionEndpointTransportClient"/> connecting through a <see cref="NamedPipeConnectionOpener"/>.<br/>
   /// Guarded so that every named-pipe transport registration can demand it without conflicting when an endpoint speaks several<br/>
   /// communication styles.</summary>
   public static IComponentRegistrar NamedPipeEndpointTransportClientIfNotRegistered(this IComponentRegistrar registrar)
      => registrar.IsRegistered<IEndpointTransportClient>()
            ? registrar
            : registrar.Register(Singleton.For<IEndpointTransportClient>().CreatedBy(() => new FramedConnectionEndpointTransportClient(new NamedPipeConnectionOpener())));
}
