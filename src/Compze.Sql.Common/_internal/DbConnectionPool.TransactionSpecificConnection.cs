using System.Transactions;
using Compze.Internals.SystemCE.ThreadingCE.Async;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Internals.SystemCE._private;

namespace Compze.Sql.Common._internal;

partial class DbConnectionPool<TConnection, TCommand>
{
   ///<summary>The one connection every operation under one transaction runs on, opened by the first of them that uses it. Its lock<br/>
   /// serializes the transaction's operations on it, the opening included: operations awaited together, say, run one at a time<br/>
   /// instead of colliding on the connection, a collision no provider supports and that MySQL rejects outright ("already an open<br/>
   /// DataReader"). The lock is re-entrant per async flow, so a nested use on the same flow does not deadlock, while independent flows<br/>
   /// serialize.</summary>
   ///<remarks>Why it opens on its first use, rather than as the pool registers it for its transaction: the pool registers it under the<br/>
   /// lock on every transaction's connection, and opening can wait, such as for the provider's own pool to hand out a connection<br/>
   /// while as many as it allows are in use. Those come back as their transactions complete, by disposing their<br/>
   /// <see cref="TransactionSpecificConnection"/>s, which the pool removes under that same lock. An open under it waited for what<br/>
   /// only its release allowed, and every transaction on the pool stalled until the open timed out.<br/>
   /// An open that fails leaves no connection: the next use opens one.</remarks>
   ///<remarks>Why the connection subscribes its disposal to the transaction's completion only once it has opened: a provider may<br/>
   /// handle the transaction's completion on the connection in a handler of its own, which it subscribes as the connection takes part<br/>
   /// in the transaction, as it opens, as SqlClient does. Subscribed after that handler, the disposal runs after it, so the connection<br/>
   /// goes back to the provider's pool only once the provider is done with the completed transaction on it. Subscribed before, as it was until<br/>
   /// 2026-11-26, the disposal put the connection back in SqlClient's pool first, and another transaction could take it at once.<br/>
   /// SqlClient's handler then doomed it in that transaction, which failed to commit with "The requested operation cannot be<br/>
   /// completed because the connection has been broken": in SqlClient 6.1.3, <c>SqlDelegatedTransaction.TransactionEnded</c> dooms<br/>
   /// the connection whatever transaction it takes part in by then.</remarks>
   sealed class TransactionSpecificConnection : IDisposable
   {
      readonly DbConnectionPool<TConnection, TCommand> _pool;
      readonly Transaction _transaction;
      readonly IAsyncLockCE _serializedAccess = IAsyncLockCE.WithDefaultTimeout();
      TConnection? _connection;

      //PROBE: operations using the connection now, and where the last one entered.
      int _probeOperationsInProgress;
      volatile string? _probeLastEntry;

      internal TransactionSpecificConnection(DbConnectionPool<TConnection, TCommand> pool, Transaction transaction)
      {
         _pool = pool;
         _transaction = transaction;
         transaction.EnlistVolatile(new ProbeCommitChecker(this), EnlistmentOptions.None);
      }

      internal TResult Locked<TResult>(Func<TConnection, TResult> func) =>
         _serializedAccess.Locked(() =>
         {
            ProbeEnter();
            try
            {
               return func(_connection ?? TheTransactionsConnection(_pool.OpenConnection()));
            }
            finally
            {
               if(_connection != null) ProbeOperationsReturned?.Invoke(_connection);
               Interlocked.Decrement(ref _probeOperationsInProgress);
            }
         });

      internal async Task<TResult> LockedAsync<TResult>(Func<TConnection, Task<TResult>> func) =>
         await _serializedAccess.LockedAsync(async () =>
         {
            ProbeEnter();
            try
            {
               return await func(_connection ?? TheTransactionsConnection(await _pool.OpenConnectionAsync().caf())).caf();
            }
            finally
            {
               if(_connection != null) ProbeOperationsReturned?.Invoke(_connection);
               Interlocked.Decrement(ref _probeOperationsInProgress);
            }
         }).caf();

      void ProbeEnter()
      {
         var inProgress = Interlocked.Increment(ref _probeOperationsInProgress);
         var previousEntry = _probeLastEntry;
         _probeLastEntry = Environment.StackTrace;
         if(inProgress > 1) ProbeLog.Write($"PROBE TransactionSpecificConnection: {inProgress} operations use the transaction's connection at once.{Environment.NewLine}==== The previous entry:{Environment.NewLine}{previousEntry}{Environment.NewLine}==== This entry:{Environment.NewLine}{_probeLastEntry}");
      }

      string? ProbeInUse() => Volatile.Read(ref _probeOperationsInProgress) == 0 ? null : $"{_probeOperationsInProgress} operation(s) in progress, the last entered at:{Environment.NewLine}{_probeLastEntry}";

      sealed class ProbeCommitChecker(TransactionSpecificConnection connection) : IEnlistmentNotification
      {
         public void Prepare(PreparingEnlistment preparingEnlistment)
         {
            if(connection.ProbeInUse() is {} inUse)
            {
               var message = $"PROBE TransactionSpecificConnection: the transaction prepares to commit while {inUse}{Environment.NewLine}==== Preparing at:{Environment.NewLine}{Environment.StackTrace}";
               ProbeLog.Write(message);
               preparingEnlistment.ForceRollback(new InvalidOperationException(message));
               return;
            }

            preparingEnlistment.Prepared();
         }

         public void Commit(Enlistment enlistment) => enlistment.Done();
         public void Rollback(Enlistment enlistment) => enlistment.Done();
         public void InDoubt(Enlistment enlistment) => enlistment.Done();
      }

      ///<summary>Keeps <paramref name="opened"/>, which has opened and taken part in the transaction, as the transaction's connection,<br/>
      /// and disposes it as the transaction completes.</summary>
      TConnection TheTransactionsConnection(TConnection opened)
      {
         _connection = opened;
         _transaction.OnCompleted(Dispose);
         return opened;
      }

      ///<summary>Disposes the connection, if one opened, once the operations using it have released the lock.</summary>
      public void Dispose() => _serializedAccess.Locked(() => _connection?.Dispose());
   }
}
