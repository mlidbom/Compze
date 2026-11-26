using System.Transactions;
using Compze.DependencyInjection._internal;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
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
      ///<remarks>On Linux, a synchronous call to a server database, such as PostgreSQL or MySQL, waits for a free thread-pool thread to<br/>
      /// receive its answer. So synchronous units of work on pool threads that block, waiting for one another, can stall one another's<br/>
      /// database calls for seconds, on Linux only. There, run such work with <see cref="ExecuteUnitOfWorkAsync{TResult}"/>.</remarks>
      public TResult ExecuteUnitOfWork<TResult>([InstantHandle]Func<IUnitOfWorkResolver, TResult> function)
      {
         using var scope = @this.BeginScope();
         var beginsATransaction = Transaction.Current == null;
         return TransactionScopeCe.Execute(() => function(UnitOfWorkIn(scope, beginsATransaction)));
      }

      ///<summary>Runs <paramref name="action"/> as its own unit of work — see <see cref="ExecuteUnitOfWork{TResult}"/>.</summary>
      public void ExecuteUnitOfWork([InstantHandle]Action<IUnitOfWorkResolver> action)
      {
         using var scope = @this.BeginScope();
         var beginsATransaction = Transaction.Current == null;
         TransactionScopeCe.Execute(() => action(UnitOfWorkIn(scope, beginsATransaction)));
      }

      ///<summary>Runs <paramref name="function"/> as its own unit of work — the async form of <see cref="ExecuteUnitOfWork{TResult}"/>:<br/>
      /// the ambient transaction flows across the function's awaits, so the whole async execution commits or rolls back as one.</summary>
      public async Task<TResult> ExecuteUnitOfWorkAsync<TResult>([InstantHandle]Func<IUnitOfWorkResolver, Task<TResult>> function)
      {
         using var scope = @this.BeginScope();
         var beginsATransaction = Transaction.Current == null;
         return await TransactionScopeCe.ExecuteAsync(async () => await function(UnitOfWorkIn(scope, beginsATransaction)).caf()).caf();
      }

      ///<summary>Runs <paramref name="action"/> as its own unit of work — see <see cref="ExecuteUnitOfWorkAsync{TResult}"/>.</summary>
      public async Task ExecuteUnitOfWorkAsync([InstantHandle]Func<IUnitOfWorkResolver, Task> action)
      {
         using var scope = @this.BeginScope();
         var beginsATransaction = Transaction.Current == null;
         await TransactionScopeCe.ExecuteAsync(async () => await action(UnitOfWorkIn(scope, beginsATransaction)).caf()).caf();
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

   ///<summary>The unit of work of <paramref name="scope"/> and the ambient transaction. When it began the transaction, it is found<br/>
   /// from the transaction alone until the transaction completes: see <see cref="AmbientUnitOfWork"/>.</summary>
   static IUnitOfWorkResolver UnitOfWorkIn(IScope scope, bool beganTheTransaction)
   {
      var unitOfWork = UnitOfWorkResolver.From(scope.Resolver);
      if(beganTheTransaction) AmbientUnitOfWork.Began(unitOfWork);
      return unitOfWork;
   }
}
