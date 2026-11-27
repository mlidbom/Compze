using System.Transactions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.SystemCE;
using JetBrains.Annotations;

namespace Compze.Internals.SystemCE.TransactionsCE;

public static class TransactionScopeCe
{
   ///<summary>The backstop timeout carried by every unit of work created here that isn't given one of its own.</summary>
   ///<remarks>A <see cref="TransactionScope"/> only reaches a terminal state — the moment its connection is disposed and every<br/>
   /// enlisted <see cref="System.Transactions.IEnlistmentNotification"/> participant gets its commit/rollback callback — when the<br/>
   /// scope is disposed, normally on the owning thread as the <c>using</c> block exits. A thread abandoned or wedged mid-unit-of-work<br/>
   /// never disposes its scope, so nothing fires those callbacks: the enlisted connection stays open holding its locks, and every<br/>
   /// participant sits un-rolled-back. The only thread-independent escape is the transaction timeout, whose timer aborts an<br/>
   /// abandoned transaction and delivers rollback to every participant. Left unset, our <see cref="TimeSpan.Zero"/> is clamped to<br/>
   /// <see cref="System.Transactions.TransactionManager.MaximumTimeout"/> (10 minutes on this runtime) — far too long a leak. This<br/>
   /// bounds it to a value well above any real unit of work here, so a wedged one self-heals in bounded time. Clean shutdown means<br/>
   /// it is essentially never reached.</remarks>
   public static readonly TimeSpan DefaultTimeout = 45.Seconds();

   static readonly TransactionOptions UnitOfWorkTransactionOptions = UnitOfWorkTransactionOptionsTimingOutAfter(DefaultTimeout);

   public static void Execute([InstantHandle] Action action, TransactionScopeOption option = TransactionScopeOption.Required) => Execute(action.ToFunc(), option);

   public static void SuppressAmbient(Action action) => Execute(action, TransactionScopeOption.Suppress);

   public static TResult Execute<TResult>([InstantHandle] Func<TResult> action, TransactionScopeOption option = TransactionScopeOption.Required)
   {
      using var transactionScope = new TransactionScope(option, UnitOfWorkTransactionOptions, TransactionScopeAsyncFlowOption.Enabled);
      var result = action();
      transactionScope.Complete();
      return result;
   }


   public static async Task SuppressAmbientAsync(Func<Task> action) => await ExecuteAsync(action, TransactionScopeOption.Suppress).caf();

   public static async Task ExecuteAsync([InstantHandle] Func<Task> action, TransactionScopeOption option = TransactionScopeOption.Required)
   {
      using var transactionScope = new TransactionScope(option, UnitOfWorkTransactionOptions, TransactionScopeAsyncFlowOption.Enabled);
      await action().caf();
      transactionScope.Complete();
   }

   ///<summary>The form of <see cref="ExecuteAsync(Func{Task},TransactionScopeOption)"/> whose transaction times out after<br/>
   /// <paramref name="timeout"/> instead of <see cref="DefaultTimeout"/>: for work that runs longer by design, or must end sooner.<br/>
   /// <see cref="TransactionManager.MaximumTimeout"/> silently shortens a longer one.</summary>
   public static async Task ExecuteAsync([InstantHandle] Func<Task> action, TimeSpan timeout, TransactionScopeOption option = TransactionScopeOption.Required)
   {
      using var transactionScope = new TransactionScope(option, UnitOfWorkTransactionOptionsTimingOutAfter(timeout), TransactionScopeAsyncFlowOption.Enabled);
      await action().caf();
      transactionScope.Complete();
   }

   public static async Task<TResult> SuppressAmbientAsync<TResult>([InstantHandle] Func<Task<TResult>> action) => await ExecuteAsync(action, TransactionScopeOption.Suppress).caf();
   public static async Task<TResult> ExecuteAsync<TResult>([InstantHandle] Func<Task<TResult>> action, TransactionScopeOption option = TransactionScopeOption.Required)
   {
      using var transactionScope = new TransactionScope(option, UnitOfWorkTransactionOptions, TransactionScopeAsyncFlowOption.Enabled);
      var result = await action().caf();
      transactionScope.Complete();
      return result;
   }

   static TransactionOptions UnitOfWorkTransactionOptionsTimingOutAfter(TimeSpan timeout) => new()
   {
      IsolationLevel = IsolationLevel.ReadCommitted,
      Timeout = timeout
   };
}
