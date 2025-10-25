using Compze.Abstractions.Tessaging.Teventive.TeventStore.Internal;
using Compze.Abstractions.Tessaging.Teventive.TeventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Infrastructure.Validation;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using JetBrains.Annotations;

namespace Compze.Tessaging.Implementation;

static class ServiceBusTeventStoreTeventPublisherRegistrar
{
   internal static IComponentRegistrar ServiceBusTeventStoreTeventPublisher(this IComponentRegistrar registrar)
      => registrar.Register(Implementation.ServiceBusTeventStoreTeventPublisher.RegisterWith);
}

[UsedImplicitly] class ServiceBusTeventStoreTeventPublisher : ITeventStoreTeventPublisher
{
   internal static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITeventStoreTeventPublisher>()
                                     .CreatedBy((IOutbox outbox, ITessageHandlerRegistry tessageHandlerRegistry)
                                                   => new ServiceBusTeventStoreTeventPublisher(outbox, tessageHandlerRegistry)));

   readonly IOutbox _outbox;
   readonly ITessageHandlerRegistry _handlerRegistry;

   public ServiceBusTeventStoreTeventPublisher(IOutbox outbox, ITessageHandlerRegistry handlerRegistry)
   {
      _outbox = outbox;
      _handlerRegistry = handlerRegistry;
   }

   void ITeventStoreTeventPublisher.Publish(ITaggregateTevent tevent)
   {
      TessageInspector.AssertValidToSendRemote(tevent);
      _handlerRegistry.CreateTeventDispatcher().Dispatch(tevent);
      _outbox.PublishTransactionally(tevent);
   }
}
