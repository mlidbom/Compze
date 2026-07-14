using Compze.TypeIdentifiers;
using Compze.Abstractions.Serialization.Internal;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.Transport;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.Transport.Abstractions;

namespace Compze.Tessaging.Transport;

public static class TessagingTransportServerRegistrar
{
   ///<summary>Registers Tessaging's request handling (<see cref="TessagingRequestHandlers"/>), contributed to the endpoint's one<br/>
   /// transport server — the protocol registration supplies the server itself.</summary>
   public static IComponentRegistrar TessagingTransportServer(this IComponentRegistrar registrar)
      => registrar.Register(TessagingRequestHandlers.RegisterWith);
}

///<summary>Tessaging's contribution to the endpoint's transport server: receives arriving exactly-once tevents and tommands<br/>
/// into the endpoint's <see cref="IInbox"/> — served identically over named pipes and HTTP.</summary>
class TessagingRequestHandlers : ITransportRequestHandlerContribution
{
   public static void RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.ForSet<ITransportRequestHandlerContribution>()
                  .CreatedBy((IInbox inbox, ITessagingSerializer serializer, ITypeMap typeMap)
                                => new TessagingRequestHandlers(inbox, serializer, typeMap)));

   public IReadOnlyDictionary<TransportRequestKind, Func<TransportRequest, Task<string>>> RequestHandlers { get; }

   TessagingRequestHandlers(IInbox inbox, ITessagingSerializer serializer, ITypeMap typeMap)
   {
      RequestHandlers = new Dictionary<TransportRequestKind, Func<TransportRequest, Task<string>>>
      {
         [TransportRequestKind.ExactlyOnceTevent] = ReceiveIntoInbox,
         [TransportRequestKind.ExactlyOnceTommand] = ReceiveIntoInbox
      };

      return;

      async Task<string> ReceiveIntoInbox(TransportRequest request)
      {
         var incomingTessage = new TransportTessage.InComing(request.Body, request.PayloadTypeIdString, request.TessageId, typeMap, serializer);
         await inbox.ReceiveAsync(incomingTessage).caf();
         return "";
      }
   }
}
