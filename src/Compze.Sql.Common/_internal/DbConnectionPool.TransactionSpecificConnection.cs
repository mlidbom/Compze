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

      internal TransactionSpecificConnection(DbConnectionPool<TConnection, TCommand> pool, Transaction transaction)
      {
         _pool = pool;
         _transaction = transaction;
      }

      ///<summary>Takes the lock, opens the connection if none has opened, and passes it to <paramref name="func"/>.</summary>
      internal TResult Locked<TResult>(Func<TConnection, TResult> func) =>
         _serializedAccess.Locked(() => ProbeUse(() => func(_connection ?? TheTransactionsConnection(_pool.OpenConnection()))));

      ///<summary>Awaits the lock, opens the connection if none has opened, and passes it to <paramref name="func"/>.</summary>
      internal async Task<TResult> LockedAsync<TResult>(Func<TConnection, Task<TResult>> func) =>
         await _serializedAccess.LockedAsync(() => ProbeUseAsync(async () => await func(_connection ?? TheTransactionsConnection(await _pool.OpenConnectionAsync().caf())).caf())).caf();

      //PROBE, experiment only: records each use that enters while another use of this connection, on its flow or the flow it
      //was started on, is still in progress.
      sealed class ProbeMarker { internal volatile bool Ended; }
      readonly AsyncLocal<ProbeMarker?> _probeUseOfThisFlow = new();

      TResult ProbeUse<TResult>(Func<TResult> use)
      {
         var outer = _probeUseOfThisFlow.Value;
         if(outer is { Ended: false })
         {
            ConnectionLockReentryProbeLog.Record("sync");
            return use();
         }
         var marker = new ProbeMarker();
         _probeUseOfThisFlow.Value = marker;
         try { return use(); }
         finally { marker.Ended = true; _probeUseOfThisFlow.Value = outer; }
      }

      async Task<TResult> ProbeUseAsync<TResult>(Func<Task<TResult>> use)
      {
         var outer = _probeUseOfThisFlow.Value;
         if(outer is { Ended: false })
         {
            ConnectionLockReentryProbeLog.Record("async");
            return await use().caf();
         }
         var marker = new ProbeMarker();
         _probeUseOfThisFlow.Value = marker;
         try { return await use().caf(); }
         finally { marker.Ended = true; }
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
      public void Dispose()
      {
         if(_probeUseOfThisFlow.Value is { Ended: false }) ConnectionLockReentryProbeLog.Record("dispose");
         _serializedAccess.Locked(() => _connection?.Dispose());
      }
   }
}
