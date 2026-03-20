using Compze.DependencyInjection.Abstractions;

namespace Compze.DependencyInjection;

public sealed class ServiceResolverWrapper(Func<Type, object> resolve) : IServiceResolver
{
   readonly Func<Type, object> _resolve = resolve;
   public object Resolve(Type serviceType) => _resolve(serviceType);
}

public sealed class ScopeResolverWrapper(Func<Type, object> resolve) : IScopeResolver
{
   readonly Func<Type, object> _resolve = resolve;
   public object Resolve(Type serviceType) => _resolve(serviceType);
}
