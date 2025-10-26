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
      var endpoint = _endpointRegistry.ServerEndpoints
                                      .Single(it => it.Address.NotNull().Uri == endPointAddress.Uri);
      var incomingTessage = tessage.ToIncoming();
      try
      {
         switch(tessage.TessageTypeEnum)
         {
            case TransportTessage.TransportTessageType.AtMostOnceTommandWithReturnValue:
               return (await endpoint.ServiceLocator.Resolve<IInbox>().Receive(incomingTessage).caf()).NotNull().CastTo<TResult>();
            case TransportTessage.TransportTessageType.NonTransactionalTuery:
               return (await endpoint.ServiceLocator
                                     .Resolve<Inbox.HandlerExecutionEngine>()
                                     .Enqueue(incomingTessage).caf())
                     .NotNull()
                     .CastTo<TResult>();
            case TransportTessage.TransportTessageType.ExactlyOnceTevent:
            case TransportTessage.TransportTessageType.AtMostOnceTommand:
            case TransportTessage.TransportTessageType.ExactlyOnceTommand:
            default:
               throw new ArgumentOutOfRangeException();
         }
      }
      catch(Exception ex)
      {
         throw new TessageDispatchingFailedException(ex.ToString());
      }
   }

   public async Task PostAsync(TransportTessage.OutGoing tessage, object realTessage, EndPointAddress endPointAddress)
   {
      var endpoint = _endpointRegistry.ServerEndpoints
                                      .Single(it => it.Address.NotNull().Uri == endPointAddress.Uri);
      var incomingTessage = tessage.ToIncoming();
      try
      {
         switch(tessage.TessageTypeEnum)
         {
            case TransportTessage.TransportTessageType.ExactlyOnceTevent:
               await endpoint.ServiceLocator.Resolve<IInbox>().Receive(incomingTessage).caf();
               return;
            case TransportTessage.TransportTessageType.AtMostOnceTommand:
               await endpoint.ServiceLocator.Resolve<IInbox>().Receive(incomingTessage).caf();
               return;
            case TransportTessage.TransportTessageType.ExactlyOnceTommand:
               await endpoint.ServiceLocator.Resolve<IInbox>().Receive(incomingTessage).caf();
               return;
            case TransportTessage.TransportTessageType.AtMostOnceTommandWithReturnValue:
            case TransportTessage.TransportTessageType.NonTransactionalTuery:
            default:
               throw new ArgumentOutOfRangeException();
         }
      }
      catch(Exception ex)
      {
         throw new TessageDispatchingFailedException(ex.ToString());
      }
   }
}
