using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Validation;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Teventive.Tevents.Public;

namespace Compze.Tessaging.Implementation.TessageHandling.Dispatching;

//todo:review: Consider the InProcess vs StrictlyLocal naming. Is this one or two things? Is either is really clear and good naming?
static class InProcessTeventPublisherRegistrar
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
      //Every tevent is wrapped before routing: a tevent published without a publisher-identifying wrapper is wrapped here, and routing operates on the wrapper's type.
      var wrappedTevent = PublisherTevent.Wrapped(tevent);
      foreach(var handler in _handlerRegistry.GetTeventHandlers(wrappedTevent.GetType()))
      {
         handler(wrappedTevent, scopeResolver);
      }
   }
}
