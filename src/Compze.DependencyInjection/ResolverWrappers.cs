using Compze.DependencyInjection.Abstractions;

namespace Compze.DependencyInjection;

public sealed class ServiceResolver(Func<Type, object> resolve, Func<Type, IEnumerable<object>> resolveSet) : IServiceResolver
{
   readonly Func<Type, object> _resolve = resolve;
   readonly Func<Type, IEnumerable<object>> _resolveSet = resolveSet;
   public object Resolve(Type serviceType) => _resolve(serviceType);
   public IEnumerable<object> ResolveSet(Type serviceType) => _resolveSet(serviceType);
}

public sealed class ScopeResolver(Func<Type, object> resolve, Func<Type, IEnumerable<object>> resolveSet) : IScopeResolver
{
   readonly Func<Type, object> _resolve = resolve;
   readonly Func<Type, IEnumerable<object>> _resolveSet = resolveSet;
   public object Resolve(Type serviceType) => _resolve(serviceType);
   public IEnumerable<object> ResolveSet(Type serviceType) => _resolveSet(serviceType);
}
