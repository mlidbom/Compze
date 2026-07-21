using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Engine._private;

namespace Compze.Tessaging.Typermedia._private;

static class SessionLocalTypermediaNavigatorRegistrar
{
   public static IComponentRegistrar LocalTypermediaNavigatorSession(this IComponentRegistrar registrar)
      => registrar.Register(Scoped.For<ILocalTypermediaNavigatorSession>()
                                  .CreatedBy((TessageHandlerExecutor executor, IScopeResolver scopeResolver)
                                                => new LocalTypermediaNavigatorSession(executor, scopeResolver)));
}
