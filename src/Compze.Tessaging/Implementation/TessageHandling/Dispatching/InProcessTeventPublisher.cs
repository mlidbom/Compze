using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Validation;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;

namespace Compze.Tessaging.Implementation.TessageHandling.Dispatching;

public static class InProcessTeventPublisherRegistrar
{
   public static IComponentRegistrar InProcessTeventPublisher(this IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<IInProcessTeventPublisher>()
                                     .CreatedBy((ITessageHandlerRegistry handlerRegistry) => new InProcessTeventPublisher(handlerRegistry)));
}

sealed class InProcessTeventPublisher(ITessageHandlerRegistry handlerRegistry) : IInProcessTeventPublisher
{
   readonly ITessageHandlerRegistry _handlerRegistry = handlerRegistry;

   public void Publish(ITevent tevent, IScopeResolver scopeResolver)
   {
      TessageValidator.AssertValidToExecuteLocally(tevent);
      foreach(var handler in _handlerRegistry.GetTeventHandlers(tevent.GetType()))
      {
         handler(tevent, scopeResolver);
      }
   }
}
