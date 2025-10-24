using Compze.Abstractions.Tessaging.Teventive.Eventstore.Public;
using Compze.Tessaging.Common;
using Compze.Tessaging.Implementation.MessageHandling.Abstractions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using JetBrains.Annotations;

namespace Compze.Tessaging.Implementation;

static class InMemoryEventStoreEventPublisherRegistrar
{
   internal static IComponentRegistrar InMemoryEventStoreEventPublisher(this IComponentRegistrar registrar)
      => registrar.Register(Implementation.InMemoryEventStoreEventPublisher.RegisterWith);
}

[UsedImplicitly] class InMemoryEventStoreEventPublisher : IEventStoreEventPublisher
{
   internal static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<IEventStoreEventPublisher>()
                                     .CreatedBy((IMessageHandlerRegistry messageHandlerRegistry)
                                                   => new InMemoryEventStoreEventPublisher(messageHandlerRegistry)));

   readonly IMessageHandlerRegistry _handlerRegistry;

   public InMemoryEventStoreEventPublisher(IMessageHandlerRegistry handlerRegistry) => _handlerRegistry = handlerRegistry;

   void IEventStoreEventPublisher.Publish(IAggregateEvent @event)
   {
      MessageInspector.AssertValidToSendRemote(@event);
      _handlerRegistry.CreateEventDispatcher().Dispatch(@event);
   }
}
