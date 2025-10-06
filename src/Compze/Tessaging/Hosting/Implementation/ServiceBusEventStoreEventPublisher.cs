using Compze.Tessaging.Common;
using Compze.Tessaging.Hosting.Implementation.Abstractions;
using Compze.Tessaging.Teventive.EventStore.Abstractions;
using JetBrains.Annotations;

namespace Compze.Tessaging.Hosting.Implementation;

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