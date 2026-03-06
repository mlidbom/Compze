using Compze.Core.Tessaging.Transport.Internal;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Abstractions.Serialization.Internal;
using Compze.Internals.SystemCE.Core.ThreadingCE.TasksCE;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation.Memory;

public static class MemoryTransportMessagePosterApiTransportClientRegistrar
{
   public static IComponentRegistrar MemoryApiTransportClient(this IComponentRegistrar registrar)
      => registrar.Register(MemoryTransportMessagePoster.RegisterWith);
}

class MemoryTransportMessagePoster : ITransportMessagePoster
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITransportMessagePoster>()
                                     .CreatedBy((ITypeMapper typeMapper, IRemotableTessageSerializer serializer) => new MemoryTransportMessagePoster(typeMapper, serializer)));

   readonly ITypeMapper _typeMapper;
   readonly IRemotableTessageSerializer _serializer;

   MemoryTransportMessagePoster(ITypeMapper typeMapper, IRemotableTessageSerializer serializer)
   {
      _typeMapper = typeMapper;
      _serializer = serializer;
   }

   public async Task<TResult> PostAsync<TResult>(TransportTessage.OutGoing tessage, EndPointAddress endPointAddress)
   {
      var incomingTessage = new TransportTessage.InComing(tessage.Body, tessage.Type, tessage.TessageId, _typeMapper, _serializer);
      return await InMemoryTransportNetwork.GetServer(endPointAddress)
                  .PostAsync<TResult>(incomingTessage).caf();
   }

   public async Task PostAsync(TransportTessage.OutGoing tessage, EndPointAddress endPointAddress)
   {
      var incomingTessage = new TransportTessage.InComing(tessage.Body, tessage.Type, tessage.TessageId, _typeMapper, _serializer);
      await InMemoryTransportNetwork.GetServer(endPointAddress)
           .PostAsync(incomingTessage).caf();
   }
}
