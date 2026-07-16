using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Typermedia.HandlerRegistration;

namespace Compze.Typermedia;

public static class SessionLocalTypermediaNavigatorRegistrar
{
   public static IComponentRegistrar SessionLocalTypermediaNavigator(this IComponentRegistrar registrar)
      => registrar.Register(Scoped.For<ISessionLocalTypermediaNavigator>()
                                  .CreatedBy((ITypermediaHandlerRegistry typermediaHandlerRegistry, IScopeResolver scopeResolver)
                                                => new SessionLocalTypermediaNavigator(typermediaHandlerRegistry, scopeResolver)));
}
