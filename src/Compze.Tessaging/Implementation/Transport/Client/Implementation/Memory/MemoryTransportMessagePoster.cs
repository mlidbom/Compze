using Compze.Core.Tessaging.Transport.Internal;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Routing.Abstractions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Serialization.Internal;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation.Memory;

public static class MemoryTransportMessagePosterApiTransportClientRegistrar
{
   public static IComponentRegistrar MemoryApiTransportClient(this IComponentRegistrar registrar)
      => registrar.Register(MemoryTransportMessagePoster.RegisterWith);
}

public class MemoryTransportMessagePoster : ITransportMessagePoster
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITransportMessagePoster>()
                                     .CreatedBy((IEndpointRegistry endpointRegistry, ITypeMapper typeMapper , IRemotableTessageSerializer serializer) => new MemoryTransportMessagePoster(endpointRegistry, typeMapper, serializer)));

   readonly IEndpointRegistry _endpointRegistry;
   readonly ITypeMapper _typeMapper;
   readonly IRemotableTessageSerializer _serializer;

   MemoryTransportMessagePoster(IEndpointRegistry registry, ITypeMapper typeMapper, IRemotableTessageSerializer serializer)
   {
      _endpointRegistry = registry;
      _typeMapper = typeMapper;
      _serializer = serializer;
   }

   public async Task<TResult> PostAsync<TResult>(TransportTessage.OutGoing tessage, object realTessage, EndPointAddress endPointAddress)
   {
      var incomingTessage = new TransportTessage.InComing(tessage.Body, tessage.Type, tessage.TessageId, _typeMapper, _serializer);
      return await GetTransport(endPointAddress)
                  .PostAsync<TResult>(incomingTessage).caf();
   }

   public async Task PostAsync(TransportTessage.OutGoing tessage, object realTessage, EndPointAddress endPointAddress)
   {
      var incomingTessage = new TransportTessage.InComing(tessage.Body, tessage.Type, tessage.TessageId, _typeMapper, _serializer);
      await GetTransport(endPointAddress)
           .PostAsync(incomingTessage).caf();
   }

   MemoryInboxTransportServer GetTransport(EndPointAddress endPointAddress)
   {
      return _endpointRegistry.ServerEndpoints
                              .Single(it => it.Address == endPointAddress)
                              .ServiceLocator
                              .Resolve<MemoryInboxTransportServer>();
   }
}
