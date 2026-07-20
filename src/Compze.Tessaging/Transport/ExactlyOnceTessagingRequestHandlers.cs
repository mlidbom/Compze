using Compze.TypeIdentifiers;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Internals.Transport.Abstractions;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.TessageBus.Internals.TessageHandling.Inbox;

namespace Compze.Tessaging.Transport;

static class ExactlyOnceTessagingRequestHandlersRegistrar
{
   ///<summary>Registers the exactly-once pipeline's request handling (<see cref="ExactlyOnceTessagingRequestHandlers"/>),<br/>
   /// contributed to the endpoint's one transport server — the protocol registration supplies the server itself.</summary>
   public static IComponentRegistrar ExactlyOnceTessagingRequestHandlers(this IComponentRegistrar registrar)
      => registrar.Register(Transport.ExactlyOnceTessagingRequestHandlers.RegisterWith);
}

///<summary>The exactly-once pipeline's contribution to the endpoint's transport server, served identically over named pipes and<br/>
/// HTTP: receives arriving exactly-once tevents and tommands into the endpoint's <see cref="IInbox"/>, where they are persisted,<br/>
/// deduped, and handled transactionally. Wired by exactly-once Tessaging alongside the inbox itself; an endpoint without the<br/>
/// exactly-once pipeline serves these request kinds with no handler at all, failing an arriving exactly-once tessage loud.</summary>
class ExactlyOnceTessagingRequestHandlers : ITransportRequestHandlerContribution
{
   public static void RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.ForSet<ITransportRequestHandlerContribution>()
                  .CreatedBy((IInbox inbox, ITessagingSerializer serializer, ITypeMap typeMap)
                                => new ExactlyOnceTessagingRequestHandlers(inbox, serializer, typeMap)));

   public IReadOnlyDictionary<TransportRequestKind, Func<TransportRequest, Task<string>>> RequestHandlers { get; }

   ExactlyOnceTessagingRequestHandlers(IInbox inbox, ITessagingSerializer serializer, ITypeMap typeMap)
   {
      RequestHandlers = new Dictionary<TransportRequestKind, Func<TransportRequest, Task<string>>>
      {
         [TransportRequestKind.ExactlyOnceTevent] = ReceiveIntoInbox,
         [TransportRequestKind.ExactlyOnceTommand] = ReceiveIntoInbox
      };

      return;

      async Task<string> ReceiveIntoInbox(TransportRequest request)
      {
         await inbox.ReceiveAsync(new TransportTessage.InComing(request.Body, request.PayloadTypeIdString, request.TessageId, typeMap, serializer)).caf();
         return "";
      }
   }
}
