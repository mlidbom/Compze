using Compze.TypeIdentifiers;
using Compze.DependencyInjection;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Internal.Transport;

namespace Compze.Tessaging.Typermedia.Internal;

static class TypermediaTransportServerRegistrar
{
   ///<summary>Registers Typermedia's request handling (<see cref="TypermediaRequestHandlers"/>), contributed to the endpoint's one<br/>
   /// transport server — the protocol registration supplies the server itself.</summary>
   public static IComponentRegistrar TypermediaTransportServer(this IComponentRegistrar registrar)
      => registrar.Register(TypermediaRequestHandlers.RegisterWith);
}

///<summary>Typermedia's contribution to the endpoint's transport server: serves remote clients' tueries and tommands through the<br/>
/// <see cref="TypermediaHandlerExecutor"/> — served identically over named pipes and HTTP.</summary>
class TypermediaRequestHandlers : ITransportRequestHandlerContribution
{
   public static void RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.ForSet<ITransportRequestHandlerContribution>()
                  .CreatedBy((TypermediaHandlerExecutor executor, ITypermediaSerializer serializer, ITypeMap typeMap)
                                => new TypermediaRequestHandlers(executor, serializer, typeMap)));

   public IReadOnlyDictionary<TransportRequestKind, Func<TransportRequest, Task<string>>> RequestHandlers { get; }

   TypermediaRequestHandlers(TypermediaHandlerExecutor executor, ITypermediaSerializer serializer, ITypeMap typeMap)
   {
      RequestHandlers = new Dictionary<TransportRequestKind, Func<TransportRequest, Task<string>>>
      {
         [TransportRequestKind.TypermediaTuery] = async request => serializer.SerializeResult(await executor.ExecuteTueryAsync(DeserializeTessage(request)).caf()),
         [TransportRequestKind.TypermediaTommandWithResult] = async request => serializer.SerializeResult(await executor.ExecuteTommandWithResultAsync(DeserializeTessage(request)).caf()),
         [TransportRequestKind.TypermediaVoidTommand] = async request =>
         {
            await executor.ExecuteVoidTommandAsync((IAtMostOnceTypermediaTommand)DeserializeTessage(request)).caf();
            return "";
         }
      };

      return;

      ITypermediaTessage DeserializeTessage(TransportRequest request) =>
         serializer.DeserializeTessage(typeMap.GetId(request.PayloadTypeIdString).Type, request.Body);
   }
}
