using System.Diagnostics;
using System.Transactions;
using Compze.Sql.Common._internal;
using Compze.Sql.Common._internal.Abstractions;
using Compze.Contracts;
using Compze.Threading;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Microsoft.Data.Sqlite;

namespace Compze.Sql.Sqlite._internal;

interface ISqliteConnectionPool : IDbConnectionPool<ICompzeSqliteConnection, SqliteCommand>
{
   static ISqliteConnectionPool CreateInstance(string connectionString) => CreateInstance(() => connectionString);
   static SqliteConnectionPool CreateInstance(Func<string> getConnectionString) => new(getConnectionString);

   ///<summary>The connection string identifying the database this pool serves — what a caller needs to derive the<br/>
   /// database's identity outside the pool's fresh-connection-per-operation model (the endpoint catalog's process lock<br/>
   /// keys its machine-local lock on it).</summary>
   string ConnectionString { get; }

   ///<summary>The pool of a SQLite database, reached through the connection string it reads at its first use. Disposing it closes<br/>
   /// the connections Microsoft.Data.Sqlite keeps open for the database, so that the process no longer holds its file.</summary>
   ///<remarks>Once disposed, it refuses each use with an <see cref="ObjectDisposedException"/>: a connection opened after the<br/>
   /// disposal would go back to Microsoft.Data.Sqlite's pool as it closed, holding the database's file for the rest of the process.</remarks>
   public class SqliteConnectionPool : ISqliteConnectionPool, IDisposable
   {
      readonly LazyCE<IDbConnectionPool<ICompzeSqliteConnection, SqliteCommand>> _pool;
      readonly LazyCE<string> _connectionString;
      bool _disposed;

      internal SqliteConnectionPool(Func<string> getConnectionString)
      {
         _connectionString = new LazyCE<string>(getConnectionString);
         _pool = new LazyCE<IDbConnectionPool<ICompzeSqliteConnection, SqliteCommand>>(
            () => DbConnectionPool<ICompzeSqliteConnection, SqliteCommand>.ForConnectionString(
               _connectionString.Value,
               ICompzeSqliteConnection.Create));
      }

      public string ConnectionString
      {
         get
         {
            Contract.State.NotDisposed(_disposed, this);
            return _connectionString.Value;
         }
      }

      ///<summary>Microsoft.Data.Sqlite keeps each connection that is closed open in a pool of its own, for the next to open the same<br/>
      /// database, so it holds the database's file until that pool is cleared. Clearing it closes those connections now, and has<br/>
      /// those still in use closed as they are closed, rather than kept.</summary>
      public void Dispose()
      {
         _disposed = true;
         if(_pool.ValueIfInitialized() == null) return;
         using var connection = new SqliteConnection(_connectionString.Value);
         SqliteConnection.ClearPool(connection);
      }

      public override string ToString() => _pool.ValueIfInitialized()?.ToString() ?? "Not initialized";

      public TResult UseConnection<TResult>(Func<ICompzeSqliteConnection, TResult> func)
      {
         Contract.State.NotDisposed(_disposed, this);
         return AutocommitTransientLockRetry.Execute(() => _pool.Value.UseConnection(func));
      }

      public async Task<TResult> UseConnectionAsync<TResult>(Func<ICompzeSqliteConnection, Task<TResult>> func)
      {
         Contract.State.NotDisposed(_disposed, this);
         return await AutocommitTransientLockRetry.ExecuteAsync(() => _pool.Value.UseConnectionAsync(func)).caf();
      }
   }

   // SQLite serializes writers. For file databases the busy handler usually absorbs contention by waiting out the
   // busy_timeout, so this retry rarely engages there. Shared-cache in-memory databases are the reason it exists:
   // SQLite does not run the busy handler for their table locks, so a contended operation returns SQLITE_LOCKED
   // immediately and would fail spuriously — we wait for the conflicting transaction to release and run it again.
   //
   // Only autocommit operations are retried. Inside an ambient transaction a transient lock invalidates the whole
   // transaction; rerunning a single operation against the same poisoned transaction cannot help. The owner of the
   // transaction redoes the entire unit of work instead (see the inbox handler execution retry loop).
   static class AutocommitTransientLockRetry
   {
      static readonly TimeSpan RetryTimeout = TimeSpan.FromSeconds(10);
      static readonly TimeSpan InitialBackoff = TimeSpan.FromMilliseconds(1);
      static readonly TimeSpan MaxBackoff = TimeSpan.FromMilliseconds(50);

      public static TResult Execute<TResult>(Func<TResult> operation)
      {
         if(Transaction.Current != null) return operation();

         var elapsed = Stopwatch.StartNew();
         var backoff = InitialBackoff;
         while(true)
         {
            try
            {
               return operation();
            }
            catch(SqliteException e) when(SqlExceptions.Sqlite.IsTransientLockConflict(e) && elapsed.Elapsed < RetryTimeout)
            {
               Thread.Sleep(backoff);
               backoff = NextBackoff(backoff);
            }
         }
      }

      public static async Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> operation)
      {
         if(Transaction.Current != null) return await operation().caf();

         var elapsed = Stopwatch.StartNew();
         var backoff = InitialBackoff;
         while(true)
         {
            try
            {
               return await operation().caf();
            }
            catch(SqliteException e) when(SqlExceptions.Sqlite.IsTransientLockConflict(e) && elapsed.Elapsed < RetryTimeout)
            {
               await Task.Delay(backoff).caf();
               backoff = NextBackoff(backoff);
            }
         }
      }

      static TimeSpan NextBackoff(TimeSpan current) => current + current < MaxBackoff ? current + current : MaxBackoff;
   }
}
