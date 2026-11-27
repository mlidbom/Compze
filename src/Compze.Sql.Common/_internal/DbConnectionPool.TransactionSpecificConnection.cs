using System.Transactions;
using Compze.Internals.SystemCE.ThreadingCE.Async;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.SystemCE.TransactionsCE;

namespace Compze.Sql.Common._internal;

partial class DbConnectionPool<TConnection, TCommand>
{
   ///<summary>The one connection every operation under one transaction runs on, opened by the first of them that uses it. Its lock<br/>
   /// serializes the transaction's operations on it, the opening included: operations awaited together, say, run one at a time<br/>
   /// instead of colliding on the connection, a collision no provider supports and that MySQL rejects outright ("already an open<br/>
   /// DataReader"). The lock refuses a use from inside another use, a nested one or one by work started inside it, with an<br/>
   /// exception at once: such a use would collide with the use it is inside.</summary>
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
   ///<remarks>Why a disposal inside a use waits for the use to end: the transaction may complete during a use, on its flow, such as<br/>
   /// when it has ended by the time the connection subscribes its disposal, which then runs at once. Disposing the connection there<br/>
   /// would dispose it under the use, and taking the lock there is a use from inside a use.</remarks>
   sealed class TransactionSpecificConnection : IDisposable
   {
      readonly DbConnectionPool<TConnection, TCommand> _pool;
      readonly Transaction _transaction;
      readonly IThrowOnReentrancyAsyncLock _serializedAccess = IThrowOnReentrancyAsyncLock.WithDefaultTimeout();
      TConnection? _connection;
      bool _theTransactionCompletedDuringAUse;

      internal TransactionSpecificConnection(DbConnectionPool<TConnection, TCommand> pool, Transaction transaction)
      {
         _pool = pool;
         _transaction = transaction;
      }

      ///<summary>Takes the lock, opens the connection if none has opened, and passes it to <paramref name="func"/>.</summary>
      internal TResult Locked<TResult>(Func<TConnection, TResult> func) =>
         _serializedAccess.Locked(() =>
         {
            try
            {
               return func(_connection ?? TheTransactionsConnection(_pool.OpenConnection()));
            }
            finally
            {
               DisposeTheConnectionIfTheTransactionCompletedDuringTheUse();
            }
         });

      ///<summary>Awaits the lock, opens the connection if none has opened, and passes it to <paramref name="func"/>.</summary>
      internal async Task<TResult> LockedAsync<TResult>(Func<TConnection, Task<TResult>> func) =>
         await _serializedAccess.LockedAsync(async () =>
         {
            try
            {
               return await func(_connection ?? TheTransactionsConnection(await _pool.OpenConnectionAsync().caf())).caf();
            }
            finally
            {
               DisposeTheConnectionIfTheTransactionCompletedDuringTheUse();
            }
         }).caf();

      ///<summary>Keeps <paramref name="opened"/>, which has opened and taken part in the transaction, as the transaction's connection,<br/>
      /// and disposes it as the transaction completes.</summary>
      TConnection TheTransactionsConnection(TConnection opened)
      {
         _connection = opened;
         _transaction.OnCompleted(Dispose);
         return opened;
      }

      ///<summary>Disposes the connection, if one opened, once the operations using it have released the lock: inside a use, as the use<br/>
      /// ends.</summary>
      public void Dispose()
      {
         if(_serializedAccess.IsHeldOnThisFlow)
         {
            _theTransactionCompletedDuringAUse = true;
         } else
         {
            _serializedAccess.Locked(DisposeTheConnection);
         }
      }

      void DisposeTheConnectionIfTheTransactionCompletedDuringTheUse()
      {
         if(_theTransactionCompletedDuringAUse) DisposeTheConnection();
      }

      void DisposeTheConnection() => _connection?.Dispose();
   }
}
