using Compze.TypeIdentifiers;
using Compze.Abstractions.Serialization.Internal;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.Transport;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Tessaging.Implementation.Transport.Abstractions;

namespace Compze.Tessaging.Transport;

public static class TessagingTransportServerRegistrar
{
   ///<summary>Registers Tessaging's request handling (<see cref="TessagingRequestHandlers"/>), contributed to the endpoint's one<br/>
   /// transport server — the protocol registration supplies the server itself.</summary>
   public static IComponentRegistrar TessagingTransportServer(this IComponentRegistrar registrar)
      => registrar.Register(TransientTeventDirectDispatcher.RegisterWith)
                  .Register(TessagingRequestHandlers.RegisterWith);
}

///<summary>Tessaging's contribution to the endpoint's transport server, served identically over named pipes and HTTP: receives<br/>
/// arriving exactly-once tevents and tommands into the endpoint's <see cref="IInbox"/>, and dispatches arriving transient tevents<br/>
/// directly to the endpoint's handlers (<see cref="TransientTeventDirectDispatcher"/> — no inbox, no dedup, no retry).</summary>
class TessagingRequestHandlers : ITransportRequestHandlerContribution
{
   public static void RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.ForSet<ITransportRequestHandlerContribution>()
                  .CreatedBy((IInbox inbox, TransientTeventDirectDispatcher transientTeventDirectDispatcher, ITessagingSerializer serializer, ITypeMap typeMap)
                                => new TessagingRequestHandlers(inbox, transientTeventDirectDispatcher, serializer, typeMap)));

   public IReadOnlyDictionary<TransportRequestKind, Func<TransportRequest, Task<string>>> RequestHandlers { get; }

   TessagingRequestHandlers(IInbox inbox, TransientTeventDirectDispatcher transientTeventDirectDispatcher, ITessagingSerializer serializer, ITypeMap typeMap)
   {
      RequestHandlers = new Dictionary<TransportRequestKind, Func<TransportRequest, Task<string>>>
      {
         [TransportRequestKind.ExactlyOnceTevent] = ReceiveIntoInbox,
         [TransportRequestKind.ExactlyOnceTommand] = ReceiveIntoInbox,
         [TransportRequestKind.TransientTevent] = DispatchDirectly
      };

      return;

      async Task<string> ReceiveIntoInbox(TransportRequest request)
      {
         await inbox.ReceiveAsync(IncomingTessageOf(request)).caf();
         return "";
      }

      //The acknowledgement is written after the handlers have executed, so one-tessage-in-flight-per-destination keeps handling in send order.
      Task<string> DispatchDirectly(TransportRequest request)
      {
         transientTeventDirectDispatcher.Dispatch(IncomingTessageOf(request));
         return Task.FromResult("");
      }

      TransportTessage.InComing IncomingTessageOf(TransportRequest request) =>
         new(request.Body, request.PayloadTypeIdString, request.TessageId, typeMap, serializer);
   }
}
