using Compze.DependencyInjection.Abstractions;

namespace Compze.DependencyInjection;

public abstract partial class DependencyInjectionContainer
{
   protected sealed class ServiceResolverWrapper(Func<Type, object> resolve) : IServiceResolver
   {
      public object Resolve(Type serviceType) => resolve(serviceType);
   }

   protected sealed class ScopeResolverWrapper(Func<Type, object> resolve) : IScopeResolver
   {
      public object Resolve(Type serviceType) => resolve(serviceType);
   }
}
