using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.SystemCE.TransactionsCE;
using JetBrains.Annotations;

namespace Compze.DependencyInjection;

public static class ServiceLocatorTransactionRunner
{
   public static TResult ExecuteTransactionInIsolatedScope<TResult>(this IServiceLocator me, [InstantHandle]Func<IScopeResolver, TResult> function)
   {
      using var scope = me.BeginScope();
      return TransactionScopeCe.Execute(() => function(scope.Resolver));
   }

   public static void ExecuteTransactionInIsolatedScope(this IServiceLocator me, [InstantHandle]Action<IScopeResolver> action)
   {
      using var scope = me.BeginScope();
      TransactionScopeCe.Execute(() => action(scope.Resolver));
   }

   public static TResult ExecuteInIsolatedScope<TResult>(this IServiceLocator me, [InstantHandle]Func<IScopeResolver, TResult> function)
   {
      using var scope = me.BeginScope();
      return function(scope.Resolver);
   }

   public static void ExecuteInIsolatedScope(this IServiceLocator me, [InstantHandle]Action<IScopeResolver> action)
   {
      using var scope = me.BeginScope();
      action(scope.Resolver);
   }

   public static async Task ExecuteInIsolatedScopeAsync(this IServiceLocator me, [InstantHandle]Func<IScopeResolver, Task> action)
   {
      using var scope = me.BeginScope();
      await action(scope.Resolver).caf();
   }
}