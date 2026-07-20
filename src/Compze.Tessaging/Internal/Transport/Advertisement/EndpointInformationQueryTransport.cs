using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.Tessaging.TessageTypes;
using Compze.TypeIdentifiers;

namespace Compze.Tessaging.Internal.Transport.Advertisement;

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

///<summary>Executes endpoint-discovery queries against a remote endpoint: serializes the query in the fixed discovery format<br/>
/// (<see cref="EndpointInformationQuerySerializer"/>) and sends it through the endpoint transport client — one implementation for every<br/>
/// protocol, since the protocol difference lives entirely in <see cref="IEndpointTransportClient"/>.</summary>
class EndpointInformationQueryTransport : IEndpointInformationQueryTransport
{
   readonly IEndpointTransportClient _transportClient;
   readonly ITypeMap _typeMap;

   internal EndpointInformationQueryTransport(IEndpointTransportClient transportClient, ITypeMap typeMap)
   {
      _transportClient = transportClient;
      _typeMap = typeMap;
   }

   public async Task<TResult> GetAsync<TResult>(ITuery<TResult> query, EndpointAddress address, CancellationToken cancellationToken = default)
   {
      var request = new TransportRequest(TransportRequestKind.EndpointDiscoveryQuery,
                                         new TessageId(),
                                         _typeMap.GetId(query.GetType()).CanonicalString,
                                         EndpointInformationQuerySerializer.SerializeQuery(query));

      var responseJson = await _transportClient.SendAsync(request, address, cancellationToken).caf();
      return EndpointInformationQuerySerializer.DeserializeResult<TResult>(responseJson);
   }
}
