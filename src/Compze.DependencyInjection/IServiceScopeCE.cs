using Compze.DependencyInjection.Abstractions;

namespace Compze.DependencyInjection;

public static class IServiceScopeCE
{
   extension(IScope @this)
   {
      public TComponent Resolve<TComponent>() where TComponent : class =>
         @this.Resolver.Resolve<TComponent>();

      public object Resolve(Type serviceType) =>
         @this.Resolver.Resolve(serviceType);

      ///<summary>
      /// Resolves every component registered as a member of the <typeparamref name="TComponent"/> component set — see
      /// <c>ForSet(...)</c>. The result order is whatever the underlying DI container's collection resolution produces.
      ///</summary>
      public IEnumerable<TComponent> ResolveSet<TComponent>() where TComponent : class =>
         @this.Resolver.ResolveSet<TComponent>();
   }
}
