using Compze.Abstractions.Tessaging.Teventive.TeventStore.Internal;
using Compze.Abstractions.Tessaging.Teventive.TeventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Infrastructure.Validation;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using JetBrains.Annotations;

namespace Compze.Tessaging.Implementation;

static class InMemoryTeventStoreTeventPublisherRegistrar
{
   internal static IComponentRegistrar InMemoryTeventStoreTeventPublisher(this IComponentRegistrar registrar)
      => registrar.Register(Implementation.InMemoryTeventStoreTeventPublisher.RegisterWith);
}

[UsedImplicitly] class InMemoryTeventStoreTeventPublisher : ITeventStoreTeventPublisher
{
   internal static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITeventStoreTeventPublisher>()
                                     .CreatedBy((ITessageHandlerRegistry tessageHandlerRegistry)
                                                   => new InMemoryTeventStoreTeventPublisher(tessageHandlerRegistry)));

   readonly ITessageHandlerRegistry _handlerRegistry;

   public InMemoryTeventStoreTeventPublisher(ITessageHandlerRegistry handlerRegistry) => _handlerRegistry = handlerRegistry;

   void ITeventStoreTeventPublisher.Publish(IAggregateTevent tevent)
   {
      TessageInspector.AssertValidToSendRemote(tevent);
      _handlerRegistry.CreateTeventDispatcher().Dispatch(tevent);
   }
}
