using System.Collections;
using Compze.DependencyInjection.Abstractions;

namespace Compze.DependencyInjection.Private;

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
