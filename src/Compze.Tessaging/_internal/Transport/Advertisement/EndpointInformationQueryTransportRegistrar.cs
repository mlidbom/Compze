using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging._private.Transport;
using Compze.Tessaging._private.Transport.Advertisement;
using Compze.TypeIdentifiers;

namespace Compze.Tessaging._internal.Transport.Advertisement;

static class EndpointInformationQueryTransportRegistrar
{
   ///<summary>Registers the endpoint-discovery query transport, which runs on the endpoint transport client<br/>
   /// (<see cref="IEndpointTransportClient"/>). Guarded so that every transport registration demands it itself — a composing<br/>
   /// layer never registers it.</summary>
   public static IComponentRegistrar EndpointInformationQueryTransportIfNotRegistered(this IComponentRegistrar registrar)
      => registrar.IsRegistered<IEndpointInformationQueryTransport>()
            ? registrar
            : registrar.Register(Singleton.For<IEndpointInformationQueryTransport>()
                                          .CreatedBy((IEndpointTransportClient transportClient, ITypeMap typeMap) => new EndpointInformationQueryTransport(transportClient, typeMap)));
}
