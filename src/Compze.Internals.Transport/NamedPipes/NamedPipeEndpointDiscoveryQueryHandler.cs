using Compze.TypeIdentifiers;

namespace Compze.Internals.Transport.NamedPipes;

///<summary>The server side of endpoint-discovery queries over named pipes: creates the<br/>
/// <see cref="TransportRequestKind.EndpointDiscoveryQuery"/> handler a <see cref="NamedPipeTransportServer"/> dispatches to.<br/>
/// The endpoint's one transport server (<see cref="NamedPipeEndpointTransportServer"/>) registers this handler itself, because every<br/>
/// endpoint answers discovery no matter what it speaks — exactly as the ASP.NET Core endpoint transport server hosts the<br/>
/// endpoint-discovery query controller.</summary>
public static class NamedPipeEndpointDiscoveryQueryHandler
{
   public static Func<TransportRequest, Task<string>> CreateFor(EndpointDiscoveryQueryExecutor executor, ITypeMap typeMap) =>
      request =>
      {
         var queryType = typeMap.GetId(request.PayloadTypeIdString).Type;
         var query = EndpointDiscoverySerializer.DeserializeQuery(queryType, request.Body);
         var result = executor.ExecuteQuery(query);
         return Task.FromResult(EndpointDiscoverySerializer.SerializeResult(result));
      };
}
