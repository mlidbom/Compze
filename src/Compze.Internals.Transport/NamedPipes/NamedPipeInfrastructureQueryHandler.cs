using Compze.TypeIdentifiers;
using Compze.Abstractions.Serialization.Internal;

namespace Compze.Internals.Transport.NamedPipes;

///<summary>The server side of infrastructure queries over named pipes: creates the<br/>
/// <see cref="NamedPipeTransportRequestKind.InfrastructureQuery"/> handler a <see cref="NamedPipeTransportServer"/> dispatches to.<br/>
/// The endpoint's one transport server (<see cref="NamedPipeEndpointTransportServer"/>) registers this handler itself, because every<br/>
/// endpoint answers discovery no matter what it speaks — exactly as the ASP.NET Core endpoint transport server hosts the<br/>
/// infrastructure-query controller.</summary>
public static class NamedPipeInfrastructureQueryHandler
{
   public static Func<NamedPipeTransportRequest, Task<string>> CreateFor(InfrastructureQueryExecutor executor, IRemotableTessageSerializer serializer, ITypeMap typeMap) =>
      request =>
      {
         var queryType = typeMap.GetId(request.PayloadTypeIdString).Type;
         var query = serializer.DeserializeTessage(queryType, request.Body);
         var result = executor.ExecuteQuery(query);
         return Task.FromResult(serializer.SerializeResponse(result));
      };
}
