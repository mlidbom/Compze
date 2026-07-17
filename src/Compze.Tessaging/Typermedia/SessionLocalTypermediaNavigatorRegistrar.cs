using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Typermedia.HandlerRegistration;

namespace Compze.Tessaging.Typermedia;

public static class SessionLocalTypermediaNavigatorRegistrar
{
   public static IComponentRegistrar LocalTypermediaNavigatorSession(this IComponentRegistrar registrar)
      => registrar.Register(Scoped.For<ILocalTypermediaNavigatorSession>()
                                  .CreatedBy((ITypermediaHandlerRegistry typermediaHandlerRegistry, IScopeResolver scopeResolver)
                                                => new LocalTypermediaNavigatorSession(typermediaHandlerRegistry, scopeResolver)));
}
