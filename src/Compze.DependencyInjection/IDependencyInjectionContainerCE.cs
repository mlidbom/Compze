using Compze.DependencyInjection.Abstractions;
using JetBrains.Annotations;

namespace Compze.DependencyInjection;

public static class IDependencyInjectionContainerCE
{
   extension(IDependencyInjectionContainer @this)
   {
      public TComponent Resolve<TComponent>() where TComponent : class =>
         @this.RootResolver.Resolve<TComponent>();

      public IServiceScope BeginScope() =>
         @this.ScopeFactory.BeginScope();

      public TResult ExecuteTransactionInIsolatedScope<TResult>([InstantHandle] Func<IScopeResolver, TResult> function) =>
         @this.ScopeFactory.ExecuteTransactionInIsolatedScope(function);

      public void ExecuteTransactionInIsolatedScope([InstantHandle] Action<IScopeResolver> action) =>
         @this.ScopeFactory.ExecuteTransactionInIsolatedScope(action);

      public TResult ExecuteInIsolatedScope<TResult>([InstantHandle] Func<IScopeResolver, TResult> function) =>
         @this.ScopeFactory.ExecuteInIsolatedScope(function);

      public void ExecuteInIsolatedScope([InstantHandle] Action<IScopeResolver> action) =>
         @this.ScopeFactory.ExecuteInIsolatedScope(action);

      public Task ExecuteInIsolatedScopeAsync([InstantHandle] Func<IScopeResolver, Task> action) =>
         @this.ScopeFactory.ExecuteInIsolatedScopeAsync(action);
   }
}
