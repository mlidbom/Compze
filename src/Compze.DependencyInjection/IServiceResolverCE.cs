using Compze.DependencyInjection.Abstractions;

namespace Compze.DependencyInjection;

public static class IServiceResolverCE
{
   extension(IServiceResolver @this)
   {
      public TComponent Resolve<TComponent>() where TComponent : class =>
         (TComponent)@this.Resolve(typeof(TComponent));
   }
}
