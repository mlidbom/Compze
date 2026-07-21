using Compze.DependencyInjection.Abstractions;

namespace Compze.DependencyInjection._private;

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
