using Compze.TypeIdentifiers;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Internals.Transport;

///<summary>Everything the endpoint's transport server serves: the union of every communication style's contributed request<br/>
/// handlers (<see cref="ITransportRequestHandlerContribution"/>), plus the <see cref="TransportRequestKind.EndpointDiscoveryQuery"/><br/>
/// handler — which the map supplies itself, because every endpoint answers discovery no matter what it speaks. One handler per<br/>
/// <see cref="TransportRequestKind"/>; the named-pipe and ASP.NET Core transport servers both dispatch through this one map.</summary>
public sealed class TransportRequestHandlerMap
{
   public static void RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<TransportRequestHandlerMap>()
                  .CreatedBy((EndpointDiscoveryQueryExecutor endpointDiscoveryQueryExecutor, ITypeMap typeMap, IComponentSet<ITransportRequestHandlerContribution> contributions)
                                => new TransportRequestHandlerMap(endpointDiscoveryQueryExecutor, typeMap, contributions)));

   readonly IReadOnlyDictionary<TransportRequestKind, Func<TransportRequest, Task<string>>> _handlers;

   internal TransportRequestHandlerMap(EndpointDiscoveryQueryExecutor endpointDiscoveryQueryExecutor, ITypeMap typeMap, IEnumerable<ITransportRequestHandlerContribution> contributions)
   {
      var handlers = new Dictionary<TransportRequestKind, Func<TransportRequest, Task<string>>>
      {
         [TransportRequestKind.EndpointDiscoveryQuery] = EndpointDiscoveryQueryHandler(endpointDiscoveryQueryExecutor, typeMap)
      };

      foreach(var contribution in contributions)
      {
         foreach(var (requestKind, handler) in contribution.RequestHandlers)
         {
            State.Assert(!handlers.ContainsKey(requestKind), () => $"Two contributions both claim to handle {requestKind} — every request kind has exactly one handler.");
            handlers.Add(requestKind, handler);
         }
      }

      _handlers = handlers;
   }

   ///<summary>Dispatches <paramref name="request"/> to the handler registered for its <see cref="TransportRequest.Kind"/> and<br/>
   /// returns the handler's response payload.</summary>
   public async Task<string> HandleAsync(TransportRequest request) => await _handlers[request.Kind](request).caf();

   ///<summary>The server side of endpoint-discovery queries: deserializes the query from the fixed discovery format<br/>
   /// (<see cref="EndpointDiscoverySerializer"/>), executes it, and serializes the result back.</summary>
   static Func<TransportRequest, Task<string>> EndpointDiscoveryQueryHandler(EndpointDiscoveryQueryExecutor executor, ITypeMap typeMap) =>
      request =>
      {
         var queryType = typeMap.GetId(request.PayloadTypeIdString).Type;
         var query = EndpointDiscoverySerializer.DeserializeQuery(queryType, request.Body);
         var result = executor.ExecuteQuery(query);
         return Task.FromResult(EndpointDiscoverySerializer.SerializeResult(result));
      };
}
