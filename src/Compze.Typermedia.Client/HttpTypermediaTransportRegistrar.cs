using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Transport;

namespace Compze.Typermedia.Client;

public static class HttpTypermediaTransportRegistrar
{
   ///<summary>Registers the client side of the Typermedia transport speaking HTTP: the Typermedia transport client<br/>
   /// (<see cref="TypermediaTransport"/>) plus the HTTP endpoint transport client and the endpoint-discovery query transport<br/>
   /// it runs on (both shared with every other communication style, so registered only if nothing else did yet).</summary>
   public static IComponentRegistrar HttpTypermediaTransport(this IComponentRegistrar registrar)
      => registrar.HttpEndpointTransportClientIfNotRegistered()
                  .EndpointDiscoveryQueryTransportIfNotRegistered()
                  .Register(TypermediaTransport.RegisterWith);
}
