using Compze.TypeIdentifiers;
using Compze.Abstractions.Serialization.Internal;
using Compze.DependencyInjection;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Internals.Transport;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Tessaging.Implementation.Transport.Abstractions;

namespace Compze.Tessaging.Transport;

static class BestEffortTessagingRequestHandlersRegistrar
{
   ///<summary>Registers the best-effort tier's request handling (<see cref="BestEffortTessagingRequestHandlers"/>), contributed to the<br/>
   /// endpoint's one transport server — the protocol registration supplies the server itself.</summary>
   public static IComponentRegistrar BestEffortTessagingRequestHandlers(this IComponentRegistrar registrar)
      => registrar.Register(BestEffortTeventDirectDispatcher.RegisterWith)
                  .Register(Transport.BestEffortTessagingRequestHandlers.RegisterWith);
}

///<summary>The best-effort tier's contribution to the endpoint's transport server, served identically over named pipes and HTTP:<br/>
/// dispatches an arriving best-effort tevent directly to the endpoint's handlers (<see cref="BestEffortTeventDirectDispatcher"/> —<br/>
/// no inbox, no dedup, no retry). The exactly-once request kinds are the exactly-once pipeline's own contribution<br/>
/// (<see cref="ExactlyOnceTessagingRequestHandlers"/>): on an endpoint that wires no inbox they have no handler, so an arriving<br/>
/// exactly-once tessage fails loud and stays undelivered on the sender — never silently downgraded.</summary>
class BestEffortTessagingRequestHandlers : ITransportRequestHandlerContribution
{
   public static void RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.ForSet<ITransportRequestHandlerContribution>()
                  .CreatedBy((BestEffortTeventDirectDispatcher bestEffortTeventDirectDispatcher, ITessagingSerializer serializer, ITypeMap typeMap)
                                => new BestEffortTessagingRequestHandlers(bestEffortTeventDirectDispatcher, serializer, typeMap)));

   public IReadOnlyDictionary<TransportRequestKind, Func<TransportRequest, Task<string>>> RequestHandlers { get; }

   BestEffortTessagingRequestHandlers(BestEffortTeventDirectDispatcher bestEffortTeventDirectDispatcher, ITessagingSerializer serializer, ITypeMap typeMap)
   {
      RequestHandlers = new Dictionary<TransportRequestKind, Func<TransportRequest, Task<string>>>
      {
         [TransportRequestKind.BestEffortTevent] = DispatchDirectly
      };

      return;

      //The acknowledgement is written after the handlers have executed, so one-tessage-in-flight-per-destination keeps handling in send order.
      async Task<string> DispatchDirectly(TransportRequest request)
      {
         await bestEffortTeventDirectDispatcher.DispatchAsync(new TransportTessage.InComing(request.Body, request.PayloadTypeIdString, request.TessageId, typeMap, serializer)).caf();
         return "";
      }
   }
}
