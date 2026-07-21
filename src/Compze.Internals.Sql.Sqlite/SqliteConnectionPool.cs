using System.Diagnostics;
using System.Transactions;
using Compze.Internals.Sql.Common;
using Compze.Internals.Sql.Common.Abstractions;
using Compze.Internals.Sql.Sqlite._private;
using Compze.Internals.SystemCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Microsoft.Data.Sqlite;
using Compze.Internals.Sql.Sqlite._internal;

namespace Compze.Internals.Sql.Sqlite;

public interface ISqliteConnectionPool : IDbConnectionPool<ICompzeSqliteConnection, SqliteCommand>
{
   static ISqliteConnectionPool CreateInstance(string connectionString) => CreateInstance(() => connectionString);
   static SqliteConnectionPool CreateInstance(Func<string> getConnectionString) => new(getConnectionString);

   public class SqliteConnectionPool : ISqliteConnectionPool
   {
      readonly LazyCE<IDbConnectionPool<ICompzeSqliteConnection, SqliteCommand>> _pool;

      internal SqliteConnectionPool(Func<string> getConnectionString)
      {
         _pool = new LazyCE<IDbConnectionPool<ICompzeSqliteConnection, SqliteCommand>>(
            () =>
            {
               var connectionString = getConnectionString();
               return DbConnectionPool<ICompzeSqliteConnection, SqliteCommand>.ForConnectionString(
                  connectionString,
                  ICompzeSqliteConnection.Create);
            });
      }

      public override string ToString() => _pool.ValueIfInitialized()?.ToString() ?? "Not initialized";
      public TResult UseConnection<TResult>(Func<ICompzeSqliteConnection, TResult> func) => AutocommitTransientLockRetry.Execute(() => _pool.Value.UseConnection(func));
      public async Task<TResult> UseConnectionAsync<TResult>(Func<ICompzeSqliteConnection, Task<TResult>> func) => await AutocommitTransientLockRetry.ExecuteAsync(() => _pool.Value.UseConnectionAsync(func)).caf();
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
