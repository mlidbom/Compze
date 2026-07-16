using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.TransactionsCE;
using JetBrains.Annotations;

namespace Compze.DependencyInjection;

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
