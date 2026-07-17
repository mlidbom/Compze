using Compze.Abstractions.Hosting.Public;
using Compze.Tessaging.Implementation.Peers;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Tessaging.Implementation.Transport.Client.Internal;
using Compze.Tessaging.Typermedia.Client;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Tessaging.Implementation.HandlerAvailability;

static class HandlerAvailabilityRegistrar
{
   internal static IComponentRegistrar HandlerAvailability(this IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<IHandlerAvailability>().CreatedBy(
            (ITessagingRouter router, IPeerRegistry peerRegistry, HandlerAvailabilityPatience patience)
               => new HandlerAvailability(router, peerRegistry, patience)));
}

#pragma warning disable CA1724 // Type name intentionally matches namespace concept
class HandlerAvailability : IHandlerAvailability
{
   ///<summary>How often a waiting send re-checks availability. The router's reconciliation converges routes at its own signal<br/>
   /// latency; this only bounds how soon a waiter notices, so it is a latency floor, not a load concern — waits are rare and bounded.</summary>
   static readonly TimeSpan RecheckInterval = TimeSpan.FromMilliseconds(50);

   readonly ITessagingRouter _router;
   readonly IPeerRegistry _peerRegistry;
   readonly HandlerAvailabilityPatience _patience;

   internal HandlerAvailability(ITessagingRouter router, IPeerRegistry peerRegistry, HandlerAvailabilityPatience patience)
   {
      _router = router;
      _peerRegistry = peerRegistry;
      _patience = patience;
   }

   //Each waiting loop classifies its patience-exhausted failure from the same snapshot whose check exhausted the patience -
   //a re-read after the final check could see the world having just become right and build a failure message that lies about it.
   //A shutdown mid-wait needs no handling of its own: the router's lookups assert the router is not stopped, and that failure
   //propagates out of the wait immediately.

   public async Task<EndpointAddress> AwaitAddressOfTypermediaHandlerForAsync(Type tessageType)
   {
      var deadline = DateTime.UtcNow + _patience.Duration;
      while(true)
      {
         var liveRoutes = _router.TypermediaRoutesFor(tessageType);
         if(liveRoutes.Count == 1) return liveRoutes[0].Address;
         if(DateTime.UtcNow >= deadline) //After the check above: the last check happens at or after the deadline, so a route that appeared in the final interval is never missed.
            throw liveRoutes.Count == 0
                     ? NoHandlerForTypermediaTypeException.BecausePatienceIsExhausted(tessageType, _patience.Duration, _peerRegistry.HandlerIdsFor(tessageType))
                     : MultipleHandlersForTypermediaTypeException.BecausePatienceIsExhausted(tessageType, _patience.Duration, [..liveRoutes.Select(route => route.HandlerEndpointId)]);
         await Task.Delay(RecheckInterval).caf();
      }
   }

   public async Task<EndpointId> AwaitBindableReceiverOfAsync(Type tommandType)
   {
      var deadline = DateTime.UtcNow + _patience.Duration;
      while(true)
      {
         //The same preference order the bind always had: the live handler - current by definition - over the sole remembered one.
         var liveConnection = _router.LiveConnectionToHandlerFor(tommandType);
         if(liveConnection != null) return liveConnection.EndpointInformation.Id;
         var rememberedHandlerIds = _peerRegistry.HandlerIdsFor(tommandType);
         if(rememberedHandlerIds.Count == 1) return rememberedHandlerIds[0];
         if(DateTime.UtcNow >= deadline) //After the checks above: the last check happens at or after the deadline, so a receiver that became bindable in the final interval is never missed.
            throw rememberedHandlerIds.Count == 0
                     ? NoHandlerForTessageTypeException.BecausePatienceIsExhausted(tommandType, _patience.Duration)
                     : MultipleHandlersForTessageTypeException.BecausePatienceIsExhausted(tommandType, _patience.Duration, rememberedHandlerIds);
         await Task.Delay(RecheckInterval).caf();
      }
   }
}
