using Compze.TypeIdentifiers;
using Compze.Abstractions.Serialization.Internal;

namespace Compze.Internals.Transport.NamedPipes;

///<summary>The server side of infrastructure queries over named pipes: creates the<br/>
/// <see cref="NamedPipeTransportRequestKind.InfrastructureQuery"/> handler a <see cref="NamedPipeTransportServer"/> dispatches to.<br/>
/// Every transport server an endpoint runs registers this handler, so discovery can bootstrap through whichever address a client knows —<br/>
/// exactly as the HTTP transport hosts its infrastructure-query controller in each paradigm's web server.</summary>
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
