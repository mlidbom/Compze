using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging._private.Transport;
using Compze.Tessaging._private.Transport.Advertisement;
using Compze.TypeIdentifiers;
using Compze.TypeIdentifiers.DependencyInjection;

namespace Compze.Tessaging._internal.Transport.Advertisement;

static class EndpointInformationQueryTransportRegistrar
{
   ///<summary>Registers the endpoint-discovery query transport, which runs on the endpoint transport client<br/>
   /// (<see cref="IEndpointTransportClient"/>). Guarded so that every transport registration demands it itself — a composing<br/>
   /// layer never registers it.</summary>
   ///<remarks>It also declares the type mappings the transport needs: the queries it sends, such as<br/>
   /// <see cref="EndpointInformationQuery"/>, are this assembly's own types, sent under their mapped type identity. Declared<br/>
   /// here, whatever composes the transport has them mapped: an endpoint, and also a pure client, which registers no<br/>
   /// tessaging serializer to declare them in passing.</remarks>
   public static IComponentRegistrar EndpointInformationQueryTransportIfNotRegistered(this IComponentRegistrar registrar)
      => registrar.IsRegistered<IEndpointInformationQueryTransport>()
            ? registrar
            : registrar.RequireMappedTypesFromAssemblyContaining<EndpointInformationQuery>()
                       .Register(Singleton.For<IEndpointInformationQueryTransport>()
                                          .CreatedBy((IEndpointTransportClient transportClient, ITypeMap typeMap) => new EndpointInformationQueryTransport(transportClient, typeMap)));
}
