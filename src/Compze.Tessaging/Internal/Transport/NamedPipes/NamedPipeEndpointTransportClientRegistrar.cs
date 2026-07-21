using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Private.Transport;
using Compze.Tessaging.Private.Transport.NamedPipes;

namespace Compze.Tessaging.Internal.Transport.NamedPipes;

static class NamedPipeEndpointTransportClientRegistrar
{
   ///<summary>Registers the named-pipe implementation of the endpoint transport's client side (<see cref="IEndpointTransportClient"/>).<br/>
   /// Guarded so that every named-pipe transport registration can demand it without conflicting when an endpoint speaks several<br/>
   /// communication styles.</summary>
   public static IComponentRegistrar NamedPipeEndpointTransportClientIfNotRegistered(this IComponentRegistrar registrar)
      => registrar.IsRegistered<IEndpointTransportClient>()
            ? registrar
            : registrar.Register(Singleton.For<IEndpointTransportClient>().CreatedBy(() => new NamedPipeEndpointTransportClient()));
}
