using Compze.Internals.Sql.Common;
using Compze.Internals.Sql.Common.Abstractions;
using Compze.Internals.SystemCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Microsoft.Data.Sqlite;

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
                  PoolableConnectionFlags.MustUseSameConnectionThroughoutATransaction,
                  ICompzeSqliteConnection.Create);
            });
      }

      public override string ToString() => _pool.ValueIfInitialized()?.ToString() ?? "Not initialized";
      public TResult UseConnection<TResult>(Func<ICompzeSqliteConnection, TResult> func) => _pool.Value.UseConnection(func);
      public async Task<TResult> UseConnectionAsync<TResult>(Func<ICompzeSqliteConnection, Task<TResult>> func) => await _pool.Value.UseConnectionAsync(func).caf();
   }
}
