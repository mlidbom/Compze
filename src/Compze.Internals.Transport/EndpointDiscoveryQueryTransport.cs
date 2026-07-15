using Compze.TypeIdentifiers;
using Compze.Abstractions.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Hosting.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Internals.Transport;

public static class EndpointDiscoveryQueryTransportRegistrar
{
   ///<summary>Registers the endpoint-discovery query transport, which runs on the endpoint transport client<br/>
   /// (<see cref="IEndpointTransportClient"/>). Guarded so that every transport registration demands it itself — a composing<br/>
   /// layer never registers it.</summary>
   public static IComponentRegistrar EndpointDiscoveryQueryTransportIfNotRegistered(this IComponentRegistrar registrar)
      => registrar.IsRegistered<IEndpointDiscoveryQueryTransport>()
            ? registrar
            : registrar.Register(Singleton.For<IEndpointDiscoveryQueryTransport>()
                                          .CreatedBy((IEndpointTransportClient transportClient, ITypeMap typeMap) => new EndpointDiscoveryQueryTransport(transportClient, typeMap)));
}

///<summary>Executes endpoint-discovery queries against a remote endpoint: serializes the query in the fixed discovery format<br/>
/// (<see cref="EndpointDiscoverySerializer"/>) and sends it through the endpoint transport client — one implementation for every<br/>
/// protocol, since the protocol difference lives entirely in <see cref="IEndpointTransportClient"/>.</summary>
class EndpointDiscoveryQueryTransport : IEndpointDiscoveryQueryTransport
{
   readonly IEndpointTransportClient _transportClient;
   readonly ITypeMap _typeMap;

   internal EndpointDiscoveryQueryTransport(IEndpointTransportClient transportClient, ITypeMap typeMap)
   {
      _transportClient = transportClient;
      _typeMap = typeMap;
   }

   public async Task<TResult> GetAsync<TResult>(ITuery<TResult> query, EndpointAddress address)
   {
      var request = new TransportRequest(TransportRequestKind.EndpointDiscoveryQuery,
                                         new TessageId(),
                                         _typeMap.GetId(query.GetType()).CanonicalString,
                                         EndpointDiscoverySerializer.SerializeQuery(query));

      var responseJson = await _transportClient.SendAsync(request, address).caf();
      return EndpointDiscoverySerializer.DeserializeResult<TResult>(responseJson);
   }
}
