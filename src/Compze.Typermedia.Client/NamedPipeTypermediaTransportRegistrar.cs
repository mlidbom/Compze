using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Transport;
using Compze.Internals.Transport.NamedPipes;

namespace Compze.Typermedia.Client;

public static class NamedPipeTypermediaTransportRegistrar
{
   ///<summary>Registers the client side of the Typermedia transport speaking named pipes: the Typermedia transport client<br/>
   /// (<see cref="TypermediaTransport"/>) plus the named-pipe endpoint transport client and the endpoint-discovery query transport<br/>
   /// it runs on (both shared with every other communication style, so registered only if nothing else did yet) — the<br/>
   /// same-machine counterpart of <see cref="HttpTypermediaTransportRegistrar.HttpTypermediaTransport"/>.</summary>
   public static IComponentRegistrar NamedPipeTypermediaTransport(this IComponentRegistrar registrar)
      => registrar.NamedPipeEndpointTransportClientIfNotRegistered()
                  .EndpointDiscoveryQueryTransportIfNotRegistered()
                  .Register(TypermediaTransport.RegisterWith);
}
