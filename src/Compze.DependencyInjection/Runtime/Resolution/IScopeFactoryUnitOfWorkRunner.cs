using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.SystemCE.TransactionsCE;
using JetBrains.Annotations;

namespace Compze.DependencyInjection.Runtime.Resolution;

// ReSharper disable once InconsistentNaming extension classes are named {TypeName}CE after the type they extend; here that type is the interface IScopeFactory
public static class IScopeFactoryUnitOfWorkRunner
{
   extension(IScopeFactory @this)
   {
      ///<summary>Runs <paramref name="function"/> as its own unit of work: one fresh <see cref="IScope"/> paired with one<br/>
      /// transaction, begun and completed together, so the work either commits as a whole or rolls back as a whole.<br/>
      /// An ambient transaction, when the caller has one, is joined rather than replaced.</summary>
      public TResult ExecuteUnitOfWork<TResult>([InstantHandle]Func<IUnitOfWorkResolver, TResult> function)
      {
         using var scope = @this.BeginScope();
         return TransactionScopeCe.Execute(() => function(UnitOfWorkResolver.From(scope.Resolver)));
      }

      ///<summary>Runs <paramref name="action"/> as its own unit of work — see <see cref="ExecuteUnitOfWork{TResult}"/>.</summary>
      public void ExecuteUnitOfWork([InstantHandle]Action<IUnitOfWorkResolver> action)
      {
         using var scope = @this.BeginScope();
         TransactionScopeCe.Execute(() => action(UnitOfWorkResolver.From(scope.Resolver)));
      }

      ///<summary>Runs <paramref name="function"/> as its own unit of work — the async form of <see cref="ExecuteUnitOfWork{TResult}"/>:<br/>
      /// the ambient transaction flows across the function's awaits, so the whole async execution commits or rolls back as one.</summary>
      public async Task<TResult> ExecuteUnitOfWorkAsync<TResult>([InstantHandle]Func<IUnitOfWorkResolver, Task<TResult>> function)
      {
         using var scope = @this.BeginScope();
         return await TransactionScopeCe.ExecuteAsync(async () => await function(UnitOfWorkResolver.From(scope.Resolver)).caf()).caf();
      }

      ///<summary>Runs <paramref name="action"/> as its own unit of work — see <see cref="ExecuteUnitOfWorkAsync{TResult}"/>.</summary>
      public async Task ExecuteUnitOfWorkAsync([InstantHandle]Func<IUnitOfWorkResolver, Task> action)
      {
         using var scope = @this.BeginScope();
         await TransactionScopeCe.ExecuteAsync(async () => await action(UnitOfWorkResolver.From(scope.Resolver)).caf()).caf();
      }

      ///<summary>Runs <paramref name="function"/> in a fresh <see cref="IScope"/> with no transaction of its own — the async<br/>
      /// form of <see cref="ExecuteInIsolatedScope{TResult}"/>: an ambient transaction, when the caller has one, flows across<br/>
      /// the function's awaits and is left as it is.</summary>
      public async Task<TResult> ExecuteInIsolatedScopeAsync<TResult>([InstantHandle]Func<IScopeResolver, Task<TResult>> function)
      {
         using var scope = @this.BeginScope();
         return await function(scope.Resolver).caf();
      }

      ///<summary>Runs <paramref name="action"/> in a fresh <see cref="IScope"/> with no transaction of its own — see <see cref="ExecuteInIsolatedScopeAsync{TResult}"/>.</summary>
      public async Task ExecuteInIsolatedScopeAsync([InstantHandle]Func<IScopeResolver, Task> action)
      {
         using var scope = @this.BeginScope();
         await action(scope.Resolver).caf();
      }

      ///<summary>Runs <paramref name="function"/> in a fresh <see cref="IScope"/> with no transaction of its own — the context<br/>
      /// for work that changes nothing, such as executing a tuery. This is deliberately not a unit of work: there is nothing to<br/>
      /// commit or roll back. An ambient transaction, when the caller has one, is left as it is, so reads join its consistency.</summary>
      public TResult ExecuteInIsolatedScope<TResult>([InstantHandle]Func<IScopeResolver, TResult> function)
      {
         using var scope = @this.BeginScope();
         return function(scope.Resolver);
      }

      ///<summary>Runs <paramref name="action"/> in a fresh <see cref="IScope"/> with no transaction of its own — see <see cref="ExecuteInIsolatedScope{TResult}"/>.</summary>
      public void ExecuteInIsolatedScope([InstantHandle]Action<IScopeResolver> action)
      {
         using var scope = @this.BeginScope();
         action(scope.Resolver);
      }
   }
}
