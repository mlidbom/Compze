using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Typermedia.HandlerRegistration;

namespace Compze.Typermedia;

public static class UnitOfWorkLocalTypermediaNavigatorRegistrar
{
   public static IComponentRegistrar UnitOfWorkLocalTypermediaNavigator(this IComponentRegistrar registrar)
      => registrar.Register(Scoped.For<IUnitOfWorkLocalTypermediaNavigator>()
                                  .CreatedBy((ITypermediaHandlerRegistry typermediaHandlerRegistry, IScopeResolver scopeResolver)
                                                => new UnitOfWorkLocalTypermediaNavigator(typermediaHandlerRegistry, scopeResolver)));
}
