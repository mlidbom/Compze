using Compze.TypeIdentifiers;
using Compze.Abstractions.Serialization.Internal;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.Transport;
using Compze.Internals.Transport.NamedPipes;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.Transport.Internal;

namespace Compze.Tessaging.Transport.NamedPipes;

///<summary>The named-pipe implementation of the Tessaging transport's server side: receives tessages into the endpoint's<br/>
/// <see cref="IInbox"/> and answers infrastructure queries — everything the ASP.NET Core inbox server's controllers do,<br/>
/// with no web stack.</summary>
///<remarks>Depends on a deferred <see cref="IServiceResolver{TService}"/> for the <see cref="IInbox"/> rather than the inbox itself:<br/>
/// the inbox owns and starts this server, so a direct dependency would be a constructor cycle. The resolver is first used when a<br/>
/// tessage arrives, which is necessarily after the inbox — the one that started us listening — exists.</remarks>
class NamedPipeInboxTransportServer : IInboxTransportServer
{
   readonly NamedPipeTransportServer _server;

   public static void RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<IInboxTransportServer>()
                  .CreatedBy((IServiceResolver<IInbox> inbox, IRemotableTessageSerializer serializer, ITypeMap typeMap, InfrastructureQueryExecutor infrastructureQueryExecutor)
                                => new NamedPipeInboxTransportServer(inbox, serializer, typeMap, infrastructureQueryExecutor)));

   NamedPipeInboxTransportServer(IServiceResolver<IInbox> inbox, IRemotableTessageSerializer serializer, ITypeMap typeMap, InfrastructureQueryExecutor infrastructureQueryExecutor)
   {
      async Task<string> ReceiveIntoInbox(NamedPipeTransportRequest request)
      {
         var incomingTessage = new TransportTessage.InComing(request.Body, request.PayloadTypeIdString, request.TessageId, typeMap, serializer);
         await inbox.Resolve().ReceiveAsync(incomingTessage).caf();
         return "";
      }

      _server = new NamedPipeTransportServer(new Dictionary<NamedPipeTransportRequestKind, Func<NamedPipeTransportRequest, Task<string>>>
      {
         [NamedPipeTransportRequestKind.ExactlyOnceTevent] = ReceiveIntoInbox,
         [NamedPipeTransportRequestKind.ExactlyOnceTommand] = ReceiveIntoInbox,
         [NamedPipeTransportRequestKind.InfrastructureQuery] = NamedPipeInfrastructureQueryHandler.CreateFor(infrastructureQueryExecutor, serializer, typeMap)
      });
   }

   public Uri Address => _server.Address.Uri;

   public async Task StartAsync() => await _server.StartAsync().caf();
   public async Task StopAsync() => await _server.StopAsync().caf();
   public async ValueTask DisposeAsync() => await _server.DisposeAsync().caf();
}
