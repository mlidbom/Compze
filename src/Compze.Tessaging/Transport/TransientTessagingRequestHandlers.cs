using Compze.TypeIdentifiers;
using Compze.Abstractions.Serialization.Internal;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Transport;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Tessaging.Implementation.Transport.Abstractions;

namespace Compze.Tessaging.Transport;

public static class TransientTessagingRequestHandlersRegistrar
{
   ///<summary>Registers the transient tier's request handling (<see cref="TransientTessagingRequestHandlers"/>), contributed to the<br/>
   /// endpoint's one transport server — the protocol registration supplies the server itself.</summary>
   public static IComponentRegistrar TransientTessagingRequestHandlers(this IComponentRegistrar registrar)
      => registrar.Register(TransientTeventDirectDispatcher.RegisterWith)
                  .Register(Transport.TransientTessagingRequestHandlers.RegisterWith);
}

///<summary>The transient tier's contribution to the endpoint's transport server, served identically over named pipes and HTTP:<br/>
/// dispatches an arriving transient tevent directly to the endpoint's handlers (<see cref="TransientTeventDirectDispatcher"/> —<br/>
/// no inbox, no dedup, no retry). The exactly-once request kinds are the exactly-once pipeline's own contribution<br/>
/// (<see cref="ExactlyOnceTessagingRequestHandlers"/>): on an endpoint that wires no inbox they have no handler, so an arriving<br/>
/// exactly-once tessage fails loud and stays undelivered on the sender — never silently downgraded.</summary>
class TransientTessagingRequestHandlers : ITransportRequestHandlerContribution
{
   public static void RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.ForSet<ITransportRequestHandlerContribution>()
                  .CreatedBy((TransientTeventDirectDispatcher transientTeventDirectDispatcher, ITessagingSerializer serializer, ITypeMap typeMap)
                                => new TransientTessagingRequestHandlers(transientTeventDirectDispatcher, serializer, typeMap)));

   public IReadOnlyDictionary<TransportRequestKind, Func<TransportRequest, Task<string>>> RequestHandlers { get; }

   TransientTessagingRequestHandlers(TransientTeventDirectDispatcher transientTeventDirectDispatcher, ITessagingSerializer serializer, ITypeMap typeMap)
   {
      RequestHandlers = new Dictionary<TransportRequestKind, Func<TransportRequest, Task<string>>>
      {
         [TransportRequestKind.TransientTevent] = DispatchDirectly
      };

      return;

      //The acknowledgement is written after the handlers have executed, so one-tessage-in-flight-per-destination keeps handling in send order.
      Task<string> DispatchDirectly(TransportRequest request)
      {
         transientTeventDirectDispatcher.Dispatch(new TransportTessage.InComing(request.Body, request.PayloadTypeIdString, request.TessageId, typeMap, serializer));
         return Task.FromResult("");
      }
   }
}
