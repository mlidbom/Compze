using Compze.Tessaging.Common;
using Compze.Tessaging.Hosting.Implementation.Abstractions;
using Compze.Tessaging.Teventive.EventStore.Abstractions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using JetBrains.Annotations;

namespace Compze.Tessaging.Hosting.Implementation;

static class ServiceBusEventStoreEventPublisherRegistrar
{
   internal static IDependencyRegistrar ServiceBusEventStoreEventPublisher(this IDependencyRegistrar registrar)
      => registrar.Register(Implementation.ServiceBusEventStoreEventPublisher.RegisterWith);
}

[UsedImplicitly] class ServiceBusEventStoreEventPublisher : IEventStoreEventPublisher
{
   internal static void RegisterWith(IDependencyRegistrar registrar)
      => registrar.Register(Singleton.For<IEventStoreEventPublisher>()
                                     .CreatedBy((IOutbox outbox, IMessageHandlerRegistry messageHandlerRegistry)
                                                   => new ServiceBusEventStoreEventPublisher(outbox, messageHandlerRegistry)));

   readonly IOutbox _transport;
   readonly IMessageHandlerRegistry _handlerRegistry;

   public ServiceBusEventStoreEventPublisher(IOutbox transport, IMessageHandlerRegistry handlerRegistry)
   {
      _transport = transport;
      _handlerRegistry = handlerRegistry;
   }

   void IEventStoreEventPublisher.Publish(IAggregateEvent @event)
   {
      MessageInspector.AssertValidToSendRemote(@event);
      _handlerRegistry.CreateEventDispatcher().Dispatch(@event);
      _transport.PublishTransactionally(@event);
   }
}
