using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.SystemCE.TransactionsCE;
using JetBrains.Annotations;

namespace Compze.DependencyInjection;

// ReSharper disable once InconsistentNaming extension classes are named {TypeName}CE after the type they extend; here that type is the interface IScopeFactory
public static class IScopeFactoryTransactionRunner
{
   extension(IScopeFactory @this)
   {
      public TResult ExecuteTransactionInIsolatedScope<TResult>([InstantHandle]Func<IScopeResolver, TResult> function)
      {
         using var scope = @this.BeginScope();
         return TransactionScopeCe.Execute(() => function(scope.Resolver));
      }

      public void ExecuteTransactionInIsolatedScope([InstantHandle]Action<IScopeResolver> action)
      {
         using var scope = @this.BeginScope();
         TransactionScopeCe.Execute(() => action(scope.Resolver));
      }

      public TResult ExecuteInIsolatedScope<TResult>([InstantHandle]Func<IScopeResolver, TResult> function)
      {
         using var scope = @this.BeginScope();
         return function(scope.Resolver);
      }

      public void ExecuteInIsolatedScope([InstantHandle]Action<IScopeResolver> action)
      {
         using var scope = @this.BeginScope();
         action(scope.Resolver);
      }

      public async Task ExecuteInIsolatedScopeAsync([InstantHandle]Func<IScopeResolver, Task> action)
      {
         using var scope = @this.BeginScope();
         await action(scope.Resolver).caf();
      }
   }
}
