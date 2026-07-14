using Compze.TypeIdentifiers;
using Compze.Abstractions.Serialization.Internal;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.Transport.NamedPipes;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.Transport.Abstractions;

namespace Compze.Tessaging.Transport.NamedPipes;

///<summary>Tessaging's contribution to the endpoint's named-pipe transport server: receives arriving exactly-once tevents and<br/>
/// tommands into the endpoint's <see cref="IInbox"/> — everything the ASP.NET Core Tessaging controller does, with no web stack.</summary>
class NamedPipeTessagingRequestHandlers : INamedPipeRequestHandlerContribution
{
   public static void RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.ForSet<INamedPipeRequestHandlerContribution>()
                  .CreatedBy((IInbox inbox, IRemotableTessageSerializer serializer, ITypeMap typeMap)
                                => new NamedPipeTessagingRequestHandlers(inbox, serializer, typeMap)));

   public IReadOnlyDictionary<NamedPipeTransportRequestKind, Func<NamedPipeTransportRequest, Task<string>>> RequestHandlers { get; }

   NamedPipeTessagingRequestHandlers(IInbox inbox, IRemotableTessageSerializer serializer, ITypeMap typeMap)
   {
      async Task<string> ReceiveIntoInbox(NamedPipeTransportRequest request)
      {
         var incomingTessage = new TransportTessage.InComing(request.Body, request.PayloadTypeIdString, request.TessageId, typeMap, serializer);
         await inbox.ReceiveAsync(incomingTessage).caf();
         return "";
      }

      RequestHandlers = new Dictionary<NamedPipeTransportRequestKind, Func<NamedPipeTransportRequest, Task<string>>>
      {
         [NamedPipeTransportRequestKind.ExactlyOnceTevent] = ReceiveIntoInbox,
         [NamedPipeTransportRequestKind.ExactlyOnceTommand] = ReceiveIntoInbox
      };
   }
}
