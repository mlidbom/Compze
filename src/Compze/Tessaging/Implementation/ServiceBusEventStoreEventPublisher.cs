using Compze.Abstractions.Tessaging.Teventive.EventStore.Public;
using Compze.Tessaging.Common;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.MessageHandling.Abstractions;
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
                                     .CreatedBy((IOutbox outbox, IMessageHandlerRegistry messageHandlerRegistry)
                                                   => new ServiceBusEventStoreEventPublisher(outbox, messageHandlerRegistry)));

   readonly IOutbox _outbox;
   readonly IMessageHandlerRegistry _handlerRegistry;

   public ServiceBusEventStoreEventPublisher(IOutbox outbox, IMessageHandlerRegistry handlerRegistry)
   {
      _outbox = outbox;
      _handlerRegistry = handlerRegistry;
   }

   void IEventStoreEventPublisher.Publish(IAggregateEvent @event)
   {
      MessageInspector.AssertValidToSendRemote(@event);
      _handlerRegistry.CreateEventDispatcher().Dispatch(@event);
      _outbox.PublishTransactionally(@event);
   }
}
