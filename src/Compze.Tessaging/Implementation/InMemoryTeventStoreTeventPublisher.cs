using Compze.Core.Tessaging.Teventive.Infrastructure.Validation;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Core.Tessaging.Teventive.TeventStore.Internal;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using JetBrains.Annotations;

namespace Compze.Tessaging.Implementation;

public static class InMemoryTeventStoreTeventPublisherRegistrar
{
   public static IComponentRegistrar InMemoryTeventStoreTeventPublisher(this IComponentRegistrar registrar)
      => registrar.Register(Implementation.InMemoryTeventStoreTeventPublisher.RegisterWith);
}

[UsedImplicitly] class InMemoryTeventStoreTeventPublisher : ITeventStoreTeventPublisher
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITeventStoreTeventPublisher>()
                                     .CreatedBy((ITessageHandlerRegistry tessageHandlerRegistry)
                                                   => new InMemoryTeventStoreTeventPublisher(tessageHandlerRegistry)));

   readonly ITessageHandlerRegistry _handlerRegistry;

   public InMemoryTeventStoreTeventPublisher(ITessageHandlerRegistry handlerRegistry) => _handlerRegistry = handlerRegistry;

   void ITeventStoreTeventPublisher.Publish(ITaggregateTevent tevent)
   {
      TessageInspector.AssertValidToSendRemote(tevent);
      _handlerRegistry.CreateTeventDispatcher().Dispatch(tevent);
   }
}
