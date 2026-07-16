using Compze.DependencyInjection.Abstractions;
using JetBrains.Annotations;

namespace Compze.DependencyInjection;

public static class IDependencyInjectionContainerCE
{
   extension(IDependencyInjectionContainer @this)
   {
      public TComponent Resolve<TComponent>() where TComponent : class =>
         @this.RootResolver.Resolve<TComponent>();

      ///<summary>
      /// Resolves every component registered as a member of the <typeparamref name="TComponent"/> component set — see
      /// <c>ForSet(...)</c>. The result order is whatever the underlying DI container's collection resolution produces.
      ///</summary>
      public IEnumerable<TComponent> ResolveSet<TComponent>() where TComponent : class =>
         @this.RootResolver.ResolveSet<TComponent>();

      public IScope BeginScope() =>
         @this.ScopeFactory.BeginScope();

      public TResult ExecuteUnitOfWork<TResult>([InstantHandle] Func<IUnitOfWorkResolver, TResult> function) =>
         @this.ScopeFactory.ExecuteUnitOfWork(function);

      public void ExecuteUnitOfWork([InstantHandle] Action<IUnitOfWorkResolver> action) =>
         @this.ScopeFactory.ExecuteUnitOfWork(action);

      public TResult ExecuteInIsolatedScope<TResult>([InstantHandle] Func<IScopeResolver, TResult> function) =>
         @this.ScopeFactory.ExecuteInIsolatedScope(function);

      public void ExecuteInIsolatedScope([InstantHandle] Action<IScopeResolver> action) =>
         @this.ScopeFactory.ExecuteInIsolatedScope(action);
   }
}
