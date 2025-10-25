using Compze.Abstractions.Tessaging.Teventive.EventStore.Internal;
using Compze.Abstractions.Tessaging.Teventive.EventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Infrastructure.Validation;
using Compze.Abstractions.Tessaging.Teventive.Public;
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

   void IEventStoreEventPublisher.Publish(IAggregateTevent tevent)
   {
      MessageInspector.AssertValidToSendRemote(tevent);
      _handlerRegistry.CreateEventDispatcher().Dispatch(tevent);
   }
}
