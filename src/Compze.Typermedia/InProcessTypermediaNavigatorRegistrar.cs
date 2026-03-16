using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Typermedia.HandlerRegistration;

namespace Compze.Typermedia;

public static class InProcessTypermediaNavigatorRegistrar
{
   public static IComponentRegistrar InProcessTypermediaNavigator(this IComponentRegistrar registrar)
      => registrar.Register(Scoped.For<IInProcessTypermediaNavigator>()
                                  .CreatedBy((ITypermediaHandlerRegistry typermediaHandlerRegistry, IScopeServiceLocator scopeServiceLocator)
                                                => new InProcessTypermediaNavigator(typermediaHandlerRegistry, scopeServiceLocator)));
}
