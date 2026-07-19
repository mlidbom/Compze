using Compze.DependencyInjection;
using Compze.DependencyInjection.Runtime.Resolution;
using Compze.DependencyInjection.Wiring.Registration;
using Compze.Tessaging.Engine;

namespace Compze.Tessaging.Typermedia;

public static class SessionLocalTypermediaNavigatorRegistrar
{
   public static IComponentRegistrar LocalTypermediaNavigatorSession(this IComponentRegistrar registrar)
      => registrar.Register(Scoped.For<ILocalTypermediaNavigatorSession>()
                                  .CreatedBy((TessageHandlerExecutor executor, IScopeResolver scopeResolver)
                                                => new LocalTypermediaNavigatorSession(executor, scopeResolver)));
}
