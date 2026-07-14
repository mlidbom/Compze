using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Transport.NamedPipes;

namespace Compze.Typermedia.Client;

public static class NamedPipeTypermediaTransportServerRegistrar
{
   ///<summary>Registers the server side of the Typermedia transport speaking named pipes: Typermedia's request handling<br/>
   /// (<see cref="TypermediaTransportServerRegistrar.TypermediaTransportServer"/>) contributed to the endpoint's one named-pipe<br/>
   /// transport server, registering the server itself if no other communication style already did.</summary>
   public static IComponentRegistrar NamedPipeTypermediaTransportServer(this IComponentRegistrar registrar)
      => registrar.NamedPipeEndpointTransportServerIfNotRegistered()
                  .TypermediaTransportServer();
}
