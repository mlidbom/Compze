using Compze.TypeIdentifiers;
using Compze.Abstractions.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Hosting.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Internals.Transport.NamedPipes;

public static class NamedPipeEndpointDiscoveryQueryTransportRegistrar
{
   ///<summary>Registers the named-pipe implementation of the endpoint-discovery query transport — the<br/>
   /// same-machine counterpart of <see cref="HttpEndpointDiscoveryQueryTransportRegistrar.HttpEndpointDiscoveryQueryTransportIfNotRegistered"/>.<br/>
   /// Guarded so that every named-pipe transport registration demands it itself — a composing layer never registers it.</summary>
   public static IComponentRegistrar NamedPipeEndpointDiscoveryQueryTransportIfNotRegistered(this IComponentRegistrar registrar)
      => registrar.IsRegistered<IEndpointDiscoveryQueryTransport>()
            ? registrar
            : registrar.Register(NamedPipeEndpointDiscoveryQueryTransportImplementation.RegisterWith);
}

class NamedPipeEndpointDiscoveryQueryTransportImplementation : IEndpointDiscoveryQueryTransport
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<IEndpointDiscoveryQueryTransport>()
                                     .CreatedBy((ITypeMap typeMap) => new NamedPipeEndpointDiscoveryQueryTransportImplementation(typeMap)));

   readonly ITypeMap _typeMap;

   NamedPipeEndpointDiscoveryQueryTransportImplementation(ITypeMap typeMap) => _typeMap = typeMap;

   public async Task<TResult> GetAsync<TResult>(IQuery<TResult> query, EndpointAddress address)
   {
      var request = new NamedPipeTransportRequest(NamedPipeTransportRequestKind.EndpointDiscoveryQuery,
                                                  new TessageId(),
                                                  _typeMap.GetId(query.GetType()).CanonicalString,
                                                  EndpointDiscoverySerializer.SerializeQuery(query));

      var responseJson = await NamedPipeTransportClient.SendAsync(request, address).caf();
      return EndpointDiscoverySerializer.DeserializeResult<TResult>(responseJson);
   }
}
