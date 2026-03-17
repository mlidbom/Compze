using Compze.DependencyInjection.Abstractions;

namespace Compze.DependencyInjection;

public static class IServiceScopeCE
{
   extension(IServiceScope @this)
   {
      public TComponent Resolve<TComponent>() where TComponent : class =>
         @this.Resolver.Resolve<TComponent>();

      public object Resolve(Type serviceType) =>
         @this.Resolver.Resolve(serviceType);
   }
}
