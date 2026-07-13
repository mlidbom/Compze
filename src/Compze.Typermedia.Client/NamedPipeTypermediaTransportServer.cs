using Compze.TypeIdentifiers;
using Compze.Abstractions.Serialization.Internal;
using Compze.Abstractions.Tessaging.Public;
using Compze.Internals.Transport;
using Compze.Internals.Transport.NamedPipes;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Typermedia.Hosting;

namespace Compze.Typermedia.Client;

public static class NamedPipeTypermediaTransportServerRegistrar
{
   ///<summary>Registers the named-pipe implementation of the Typermedia transport server — the same-machine, no-web-stack counterpart of the ASP.NET Core typermedia server.</summary>
   public static IComponentRegistrar NamedPipeTypermediaTransportServer(this IComponentRegistrar registrar)
      => registrar.Register(Client.NamedPipeTypermediaTransportServer.RegisterWith);
}

///<summary>The named-pipe implementation of <see cref="ITypermediaTransportServer"/>: serves remote clients' tueries and tommands<br/>
/// through the <see cref="TypermediaHandlerExecutor"/> and answers infrastructure queries — everything the ASP.NET Core typermedia<br/>
/// server's controllers do, with no web stack.</summary>
class NamedPipeTypermediaTransportServer : ITypermediaTransportServer
{
   readonly NamedPipeTransportServer _server;

   public static void RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<ITypermediaTransportServer>()
                  .CreatedBy((TypermediaHandlerExecutor executor, IRemotableTessageSerializer serializer, ITypeMap typeMap, InfrastructureQueryExecutor infrastructureQueryExecutor)
                                => new NamedPipeTypermediaTransportServer(executor, serializer, typeMap, infrastructureQueryExecutor)));

   NamedPipeTypermediaTransportServer(TypermediaHandlerExecutor executor, IRemotableTessageSerializer serializer, ITypeMap typeMap, InfrastructureQueryExecutor infrastructureQueryExecutor)
   {
      ITessage DeserializeTessage(NamedPipeTransportRequest request) =>
         (ITessage)serializer.DeserializeTessage(typeMap.GetId(request.PayloadTypeIdString).Type, request.Body);

      _server = new NamedPipeTransportServer(new Dictionary<NamedPipeTransportRequestKind, Func<NamedPipeTransportRequest, Task<string>>>
      {
         [NamedPipeTransportRequestKind.TypermediaTuery] = request => Task.FromResult(serializer.SerializeResponse(executor.ExecuteTuery(DeserializeTessage(request)))),
         [NamedPipeTransportRequestKind.TypermediaTommandWithResult] = request => Task.FromResult(serializer.SerializeResponse(executor.ExecuteTommandWithResult(DeserializeTessage(request)))),
         [NamedPipeTransportRequestKind.TypermediaVoidTommand] = request =>
         {
            executor.ExecuteVoidTommand((IAtMostOnceTypermediaTommand)DeserializeTessage(request));
            return Task.FromResult("");
         },
         [NamedPipeTransportRequestKind.InfrastructureQuery] = NamedPipeInfrastructureQueryHandler.CreateFor(infrastructureQueryExecutor, serializer, typeMap)
      });
   }

   public Uri Address => _server.Address.Uri;

   public async Task StartAsync() => await _server.StartAsync().caf();
   public async Task StopAsync() => await _server.StopAsync().caf();
   public async ValueTask DisposeAsync() => await _server.DisposeAsync().caf();
}
