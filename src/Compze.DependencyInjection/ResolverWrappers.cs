using Compze.DependencyInjection.Abstractions;

namespace Compze.DependencyInjection;

public sealed class ServiceResolverWrapper(Func<Type, object> resolve) : IServiceResolver
{
   public object Resolve(Type serviceType) => resolve(serviceType);
}

public sealed class ScopeResolverWrapper(Func<Type, object> resolve) : IScopeResolver
{
   public object Resolve(Type serviceType) => resolve(serviceType);
}
