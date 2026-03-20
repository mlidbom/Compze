using Compze.Core.Tessaging.Teventive.Infrastructure.Validation;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Core.Tessaging.Teventive.TeventStore.Internal;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using JetBrains.Annotations;

namespace Compze.Tessaging.Implementation;

static class ServiceBusTeventStoreTeventPublisherRegistrar
{
   public static IComponentRegistrar ServiceBusTeventStoreTeventPublisher(this IComponentRegistrar registrar)
      => registrar.Register(Implementation.ServiceBusTeventStoreTeventPublisher.RegisterWith);
}

[UsedImplicitly] class ServiceBusTeventStoreTeventPublisher(IOutbox outbox, ITessageHandlerRegistry handlerRegistry) : ITeventStoreTeventPublisher
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITeventStoreTeventPublisher>()
                                     .CreatedBy((IOutbox outbox, ITessageHandlerRegistry tessageHandlerRegistry)
                                                   => new ServiceBusTeventStoreTeventPublisher(outbox, tessageHandlerRegistry)));

   readonly IOutbox _outbox = outbox;
   readonly ITessageHandlerRegistry _handlerRegistry = handlerRegistry;

   void ITeventStoreTeventPublisher.Publish(ITaggregateTevent tevent, IScopeResolver scopeResolver)
   {
      TessageInspector.AssertValidToSendRemote(tevent);
      _handlerRegistry.DispatchTevent(tevent, scopeResolver);
      _outbox.PublishTransactionally(tevent);
   }
}
