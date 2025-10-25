using Compze.Abstractions.Tessaging.Teventive.EventStore.Internal;
using Compze.Abstractions.Tessaging.Teventive.EventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Infrastructure.Validation;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using JetBrains.Annotations;

namespace Compze.Tessaging.Implementation;

static class ServiceBusEventStoreEventPublisherRegistrar
{
   internal static IComponentRegistrar ServiceBusEventStoreEventPublisher(this IComponentRegistrar registrar)
      => registrar.Register(Implementation.ServiceBusEventStoreEventPublisher.RegisterWith);
}

[UsedImplicitly] class ServiceBusEventStoreEventPublisher : IEventStoreEventPublisher
{
   internal static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<IEventStoreEventPublisher>()
                                     .CreatedBy((IOutbox outbox, ITessageHandlerRegistry tessageHandlerRegistry)
                                                   => new ServiceBusEventStoreEventPublisher(outbox, tessageHandlerRegistry)));

   readonly IOutbox _outbox;
   readonly ITessageHandlerRegistry _handlerRegistry;

   public ServiceBusEventStoreEventPublisher(IOutbox outbox, ITessageHandlerRegistry handlerRegistry)
   {
      _outbox = outbox;
      _handlerRegistry = handlerRegistry;
   }

   void IEventStoreEventPublisher.Publish(IAggregateTevent tevent)
   {
      TessageInspector.AssertValidToSendRemote(tevent);
      _handlerRegistry.CreateEventDispatcher().Dispatch(tevent);
      _outbox.PublishTransactionally(tevent);
   }
}
