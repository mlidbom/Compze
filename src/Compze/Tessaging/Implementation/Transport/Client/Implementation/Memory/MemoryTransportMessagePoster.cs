using Compze.Core.Tessaging.Transport.Internal;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Routing.Abstractions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using System;
using System.Linq;
using System.Threading.Tasks;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Tessaging.Implementation.TessageHandling.Inbox;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Threading.TasksCE;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation.Memory;

static class MemoryTransportMessagePosterApiTransportClientRegistrar
{
   internal static IComponentRegistrar MemoryApiTransportClient(this IComponentRegistrar registrar)
      => registrar.Register(MemoryTransportMessagePoster.RegisterWith);
}

class MemoryTransportMessagePoster : ITransportMessagePoster
{
   internal static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITransportMessagePoster>()
                                     .CreatedBy((IEndpointRegistry endpointRegistry) => new MemoryTransportMessagePoster(endpointRegistry)));

   readonly IEndpointRegistry _endpointRegistry;

   MemoryTransportMessagePoster(IEndpointRegistry registry) =>
      _endpointRegistry = registry;

   public async Task<TResult> PostAsync<TResult>(TransportTessage.OutGoing tessage, object realTessage, EndPointAddress endPointAddress)
   {
      return await _endpointRegistry.ServerEndpoints
                                    .Single(it => it.Address == endPointAddress)
                                    .ServiceLocator
                                    .Resolve<MemoryInboxTransportServer>()
                                    .PostAsync<TResult>(tessage, realTessage, endPointAddress).caf();
   }

   public async Task PostAsync(TransportTessage.OutGoing tessage, object realTessage, EndPointAddress endPointAddress)
   {
      await _endpointRegistry.ServerEndpoints
                                      .Single(it => it.Address.NotNull().Uri == endPointAddress.Uri)
                                      .ServiceLocator
                                      .Resolve<MemoryInboxTransportServer>()
                                      .PostAsync(tessage, realTessage, endPointAddress).caf();
   }
}
