using Compze.TypeIdentifiers;
using Compze.Abstractions.Serialization.Internal;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.Transport;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.Transport.Abstractions;

namespace Compze.Tessaging.Transport.NamedPipes;

///<summary>Tessaging's contribution to the endpoint's named-pipe transport server: receives arriving exactly-once tevents and<br/>
/// tommands into the endpoint's <see cref="IInbox"/> — everything the ASP.NET Core Tessaging controller does, with no web stack.</summary>
class NamedPipeTessagingRequestHandlers : ITransportRequestHandlerContribution
{
   public static void RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.ForSet<ITransportRequestHandlerContribution>()
                  .CreatedBy((IInbox inbox, ITessagingSerializer serializer, ITypeMap typeMap)
                                => new NamedPipeTessagingRequestHandlers(inbox, serializer, typeMap)));

   public IReadOnlyDictionary<TransportRequestKind, Func<TransportRequest, Task<string>>> RequestHandlers { get; }

   NamedPipeTessagingRequestHandlers(IInbox inbox, ITessagingSerializer serializer, ITypeMap typeMap)
   {
      async Task<string> ReceiveIntoInbox(TransportRequest request)
      {
         var incomingTessage = new TransportTessage.InComing(request.Body, request.PayloadTypeIdString, request.TessageId, typeMap, serializer);
         await inbox.ReceiveAsync(incomingTessage).caf();
         return "";
      }

      RequestHandlers = new Dictionary<TransportRequestKind, Func<TransportRequest, Task<string>>>
      {
         [TransportRequestKind.ExactlyOnceTevent] = ReceiveIntoInbox,
         [TransportRequestKind.ExactlyOnceTommand] = ReceiveIntoInbox
      };
   }
}
