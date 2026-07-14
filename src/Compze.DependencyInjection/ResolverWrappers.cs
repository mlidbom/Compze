using System.Collections;
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

///<summary>
/// The concrete <see cref="IServiceResolver{TService}"/>: a thin, typed view over the <see cref="IServiceResolver"/><br/>
/// that constructed the holder. <see cref="Resolve"/> just forwards to that resolver, so it stays transparent to<br/>
/// <typeparamref name="TService"/>'s lifestyle and scope. Created by the registration a <c>WithServiceResolver()</c> call adds.
///</summary>
sealed class ServiceResolver<TService>(IServiceResolver serviceResolver) : IServiceResolver<TService> where TService : class
{
   readonly IServiceResolver _serviceResolver = serviceResolver;
   public TService Resolve() => _serviceResolver.Resolve<TService>();
}

///<summary>
/// The concrete <see cref="IComponentSet{TService}"/>: a thin, typed view over the <see cref="IServiceResolver"/> that<br/>
/// constructed the holder. Enumeration forwards to <see cref="IServiceResolver.ResolveSet"/>, so it stays transparent to the<br/>
/// members' lifestyles and scope. Created by the registration <see cref="ContainerBuilder"/> synthesizes for each component-set<br/>
/// service type — and being the only type allowed to implement <see cref="IComponentSet{TService}"/> in a registration, it is<br/>
/// also how the builder recognizes its own synthesized registrations when containers are cloned or given children.
///</summary>
sealed class ComponentSet<TService>(IServiceResolver serviceResolver) : IComponentSet<TService> where TService : class
{
   readonly IServiceResolver _serviceResolver = serviceResolver;
   public IEnumerator<TService> GetEnumerator() => _serviceResolver.ResolveSet<TService>().GetEnumerator();
   IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
