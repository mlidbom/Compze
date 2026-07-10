using Compze.DependencyInjection.Abstractions;

namespace Compze.DependencyInjection;

public sealed class ServiceResolver(Func<Type, object> resolve) : IServiceResolver
{
   readonly Func<Type, object> _resolve = resolve;
   public object Resolve(Type serviceType) => _resolve(serviceType);
}

public sealed class ScopeResolver(Func<Type, object> resolve) : IScopeResolver
{
   readonly Func<Type, object> _resolve = resolve;
   public object Resolve(Type serviceType) => _resolve(serviceType);
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
