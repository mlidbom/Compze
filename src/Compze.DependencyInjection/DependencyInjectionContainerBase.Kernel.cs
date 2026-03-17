namespace Compze.DependencyInjection;

public abstract partial class DependencyInjectionContainerBase
{
   protected sealed class ServiceLocatorKernel(Func<Type, object> resolve) : IServiceLocatorKernel
   {
      readonly Func<Type, object> _nativeResolver = resolve;

      public TComponent Resolve<TComponent>() where TComponent : class =>
         (TComponent)_nativeResolver(typeof(TComponent));
   }

   protected sealed class ScopeServiceLocator(Func<Type, object> resolve) : IScopeServiceLocator
   {
      readonly Func<Type, object> _nativeResolver = resolve;

      public TComponent Resolve<TComponent>() where TComponent : class =>
         (TComponent)_nativeResolver(typeof(TComponent));
   }
}
