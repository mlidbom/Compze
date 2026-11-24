using Compze.Internals.SystemCE.ThreadingCE.Async;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

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
   sealed class TransactionSpecificConnection : IDisposable
   {
      readonly DbConnectionPool<TConnection, TCommand> _pool;
      readonly IAsyncLockCE _serializedAccess = IAsyncLockCE.WithDefaultTimeout();
      TConnection? _connection;

      internal TransactionSpecificConnection(DbConnectionPool<TConnection, TCommand> pool) => _pool = pool;

      ///<summary>Takes the lock, opens the connection if none has opened, and passes it to <paramref name="func"/>.</summary>
      internal TResult Locked<TResult>(Func<TConnection, TResult> func) =>
         _serializedAccess.Locked(() => func(_connection ??= _pool.OpenConnection()));

      ///<summary>Awaits the lock, opens the connection if none has opened, and passes it to <paramref name="func"/>.</summary>
      internal async Task<TResult> LockedAsync<TResult>(Func<TConnection, Task<TResult>> func) =>
         await _serializedAccess.LockedAsync(async () => await func(_connection ??= await _pool.OpenConnectionAsync().caf()).caf()).caf();

      ///<summary>Disposes the connection, if one opened, once the operations using it have released the lock.</summary>
      public void Dispose() => _serializedAccess.Locked(() => _connection?.Dispose());
   }
}
