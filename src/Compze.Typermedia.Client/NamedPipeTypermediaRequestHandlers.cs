using Compze.TypeIdentifiers;
using Compze.Abstractions.Serialization.Internal;
using Compze.Abstractions.Tessaging.Public;
using Compze.Internals.Transport.NamedPipes;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Typermedia.Hosting;

namespace Compze.Typermedia.Client;

public static class NamedPipeTypermediaTransportServerRegistrar
{
   ///<summary>Registers the server side of the named-pipe Typermedia transport: Typermedia's request handling contributed to the<br/>
   /// endpoint's one named-pipe transport server (registering the server itself if no other communication style already did) —<br/>
   /// the same-machine, no-web-stack counterpart of the ASP.NET Core typermedia server side.</summary>
   public static IComponentRegistrar NamedPipeTypermediaTransportServer(this IComponentRegistrar registrar)
      => registrar.NamedPipeEndpointTransportServerIfNotRegistered()
                  .Register(NamedPipeTypermediaRequestHandlers.RegisterWith);
}

///<summary>Typermedia's contribution to the endpoint's named-pipe transport server: serves remote clients' tueries and tommands<br/>
/// through the <see cref="TypermediaHandlerExecutor"/> — everything the ASP.NET Core typermedia controller does, with no web stack.</summary>
class NamedPipeTypermediaRequestHandlers : INamedPipeRequestHandlerContribution
{
   public static void RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.ForSet<INamedPipeRequestHandlerContribution>()
                  .CreatedBy((TypermediaHandlerExecutor executor, IRemotableTessageSerializer serializer, ITypeMap typeMap)
                                => new NamedPipeTypermediaRequestHandlers(executor, serializer, typeMap)));

   public IReadOnlyDictionary<NamedPipeTransportRequestKind, Func<NamedPipeTransportRequest, Task<string>>> RequestHandlers { get; }

   NamedPipeTypermediaRequestHandlers(TypermediaHandlerExecutor executor, IRemotableTessageSerializer serializer, ITypeMap typeMap)
   {
      ITessage DeserializeTessage(NamedPipeTransportRequest request) =>
         (ITessage)serializer.DeserializeTessage(typeMap.GetId(request.PayloadTypeIdString).Type, request.Body);

      RequestHandlers = new Dictionary<NamedPipeTransportRequestKind, Func<NamedPipeTransportRequest, Task<string>>>
      {
         [NamedPipeTransportRequestKind.TypermediaTuery] = request => Task.FromResult(serializer.SerializeResponse(executor.ExecuteTuery(DeserializeTessage(request)))),
         [NamedPipeTransportRequestKind.TypermediaTommandWithResult] = request => Task.FromResult(serializer.SerializeResponse(executor.ExecuteTommandWithResult(DeserializeTessage(request)))),
         [NamedPipeTransportRequestKind.TypermediaVoidTommand] = request =>
         {
            executor.ExecuteVoidTommand((IAtMostOnceTypermediaTommand)DeserializeTessage(request));
            return Task.FromResult("");
         }
      };
   }
}
