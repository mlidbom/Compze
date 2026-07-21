using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Internal.Transport.Advertisement;
using Compze.TypeIdentifiers;
using Compze.Tessaging.Private.Transport.Advertisement;
using Compze.Tessaging.Private.Transport;

namespace Compze.Tessaging.Internal.Transport;

///<summary>Everything the endpoint's transport server serves: the union of every communication style's contributed request<br/>
/// handlers (<see cref="ITransportRequestHandlerContribution"/>), plus the <see cref="TransportRequestKind.EndpointDiscoveryQuery"/><br/>
/// handler — which the map supplies itself, because every endpoint answers discovery no matter what it speaks. One handler per<br/>
/// <see cref="TransportRequestKind"/>; the named-pipe and ASP.NET Core transport servers both dispatch through this one map.</summary>
sealed class TransportRequestHandlerMap
{
   public static void RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<TransportRequestHandlerMap>()
                  .CreatedBy((EndpointInformationQueryExecutor endpointDiscoveryQueryExecutor, ITypeMap typeMap, IComponentSet<ITransportRequestHandlerContribution> contributions)
                                => new TransportRequestHandlerMap(endpointDiscoveryQueryExecutor, typeMap, contributions)));

   readonly IReadOnlyDictionary<TransportRequestKind, Func<TransportRequest, Task<string>>> _handlers;

   TransportRequestHandlerMap(EndpointInformationQueryExecutor endpointDiscoveryQueryExecutor, ITypeMap typeMap, IEnumerable<ITransportRequestHandlerContribution> contributions)
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
   /// returns the handler's response payload. A kind no contribution handles fails loud, naming the gap: it means a peer sent this<br/>
   /// endpoint a request its composition does not wire the capability to serve — the request must fail on the sender, never be<br/>
   /// silently dropped here.</summary>
   public async Task<string> HandleAsync(TransportRequest request)
   {
      if(!_handlers.TryGetValue(request.Kind, out var handler))
         throw new InvalidOperationException($"This endpoint's transport server has no handler for {request.Kind} requests: the endpoint's composition does not wire the capability that serves them. The kinds it serves: {string.Join(", ", _handlers.Keys)}.");
      return await handler(request).caf();
   }

   ///<summary>The server side of endpoint-discovery queries: deserializes the query from the fixed discovery format<br/>
   /// (<see cref="EndpointInformationQuerySerializer"/>), executes it, and serializes the result back.</summary>
   static Func<TransportRequest, Task<string>> EndpointDiscoveryQueryHandler(EndpointInformationQueryExecutor executor, ITypeMap typeMap) =>
      request =>
      {
         var queryType = typeMap.GetId(request.PayloadTypeIdString).Type;
         var query = EndpointInformationQuerySerializer.DeserializeQuery(queryType, request.Body);
         var result = executor.ExecuteQuery(query);
         return Task.FromResult(EndpointInformationQuerySerializer.SerializeResult(result));
      };
}
