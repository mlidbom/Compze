using System;
using System.Threading.Tasks;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Routing.Abstractions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

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

   public async Task<TResult> PostAsync<TResult>(TransportTessage.OutGoing tessage, object realTessage, Uri requestUri)
   {
      switch(tessage.TessageTypeEnum)
      {
         case TransportTessage.TransportTessageType.ExactlyOnceTevent:
            break;
         case TransportTessage.TransportTessageType.AtMostOnceTommand:
            break;
         case TransportTessage.TransportTessageType.AtMostOnceTommandWithReturnValue:
            break;
         case TransportTessage.TransportTessageType.ExactlyOnceTommand:
            break;
         case TransportTessage.TransportTessageType.NonTransactionalTuery:
            break;
         default:
            throw new ArgumentOutOfRangeException();
      }

      throw new NotImplementedException();
   }

   public async Task PostAsync(TransportTessage.OutGoing tessage, object realTessage, Uri requestUri) =>
      throw new NotImplementedException("");

}
