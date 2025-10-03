using Compze.EventStore.Abstractions;
using Compze.Persistence.EventStore;
using Compze.Tessaging.Common;
using JetBrains.Annotations;

namespace Compze.Tessaging.Buses.Implementation;

[UsedImplicitly] class ServiceBusEventStoreEventPublisher(IOutbox transport, IMessageHandlerRegistry handlerRegistry) : IEventStoreEventPublisher
{
   readonly IOutbox _transport = transport;
   readonly IMessageHandlerRegistry _handlerRegistry = handlerRegistry;

   void IEventStoreEventPublisher.Publish(IAggregateEvent @event)
   {
      MessageInspector.AssertValidToSendRemote(@event);
      _handlerRegistry.CreateEventDispatcher().Dispatch(@event);
      _transport.PublishTransactionally(@event);
   }
}