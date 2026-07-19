using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Engine;

namespace Compze.Tessaging.Typermedia;

static class SessionLocalTypermediaNavigatorRegistrar
{
   public static IComponentRegistrar LocalTypermediaNavigatorSession(this IComponentRegistrar registrar)
      => registrar.Register(Scoped.For<ILocalTypermediaNavigatorSession>()
                                  .CreatedBy((TessageHandlerExecutor executor, IScopeResolver scopeResolver)
                                                => new LocalTypermediaNavigatorSession(executor, scopeResolver)));
}
