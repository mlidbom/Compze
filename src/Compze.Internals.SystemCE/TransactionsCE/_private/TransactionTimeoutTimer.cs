using System.Transactions;
using Compze.Threading;

namespace Compze.Internals.SystemCE.TransactionsCE._private;

///<summary>Rolls a transaction back once its timeout has passed, as .NET's own timeout does: the transaction aborts with a<br/>
/// <see cref="TimeoutException"/> as the cause, and completing it then throws a <see cref="TransactionAbortedException"/>.</summary>
///<remarks>Why: .NET can lose a transaction's timeout, so that the transaction stays active and commits long after it. .NET keeps<br/>
/// the timeouts in a list it walks on a timer, and threads that start transactions with new timeouts at the same moment can<br/>
/// break that list, leaving every transaction beyond the break with no timeout: dotnet/runtime#136986, in .NET 11.0.0. A timer of<br/>
/// its own for each transaction can't be lost that way. A scope that joins an ambient transaction has one already, in .NET itself,<br/>
/// and this one only repeats it.</remarks>
///<remarks>Disposing it before the transaction completes keeps it from rolling back what then completes. Disposing it waits for a<br/>
/// rollback already begun, so a rollback always comes before the transaction's commit, never during it: .NET rolls back an<br/>
/// active transaction, and does nothing to one already aborted.</remarks>
sealed class TransactionTimeoutTimer : IDisposable
{
   //todo: delete this type, and its use in TransactionScopeCe.ExecuteAsync, once Compze targets a .NET release with the fix of
   //   dotnet/runtime#136986, "Transaction timeout is lost when many threads start transactions at the same time": the pull request
   //   dotnet/runtime#136370, open on 2026-11-27. .NET's own timeout then holds.
   readonly IMonitor _monitor = IMonitor.New(LockTimeout.Seconds(30));
   readonly Transaction _transaction;
   readonly TimeSpan _timeout;
   readonly Timer _timer;
   bool _isDisposed;

   TransactionTimeoutTimer(Transaction transaction, TimeSpan timeout)
   {
      _transaction = transaction;
      _timeout = timeout;
      _timer = new Timer(_ => RollBackTheTransaction(), null, timeout, Timeout.InfiniteTimeSpan);
   }

   ///<summary>A timer that rolls <paramref name="transaction"/> back once the timeout .NET gives a transaction created with<br/>
   /// <paramref name="timeout"/> has passed. Null when there is no transaction, or that timeout is none.</summary>
   internal static TransactionTimeoutTimer? For(Transaction? transaction, TimeSpan timeout) =>
      transaction is not null && TheTimeoutDotNetGivesATransactionCreatedWith(timeout) is {} itsTimeout
         ? new TransactionTimeoutTimer(transaction, itsTimeout)
         : null;

   ///<summary>The timeout of a transaction created with <paramref name="timeout"/>: <see cref="TransactionManager.MaximumTimeout"/> for a<br/>
   /// longer one, or for zero, unless the maximum is zero, which means there is none. Null for none.</summary>
   static TimeSpan? TheTimeoutDotNetGivesATransactionCreatedWith(TimeSpan timeout)
   {
      var maximum = TransactionManager.MaximumTimeout;
      if(maximum == TimeSpan.Zero) return timeout == TimeSpan.Zero ? null : timeout;
      return timeout == TimeSpan.Zero || timeout > maximum ? maximum : timeout;
   }

   void RollBackTheTransaction() => _monitor.Locked(() =>
   {
      if(!_isDisposed) _transaction.Rollback(new TimeoutException($"The transaction's timeout of {_timeout} passed before it completed."));
   });

   public void Dispose()
   {
      _monitor.Locked(() => _isDisposed = true);
      _timer.Dispose();
   }
}
