using System;
using System.Threading.Tasks;
using Compze.Sql.Common;
using Compze.Sql.Common.Abstractions;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;
using Microsoft.Data.Sqlite;

namespace Compze.Sql.Sqlite;

public interface ISqliteConnectionPool : IDbConnectionPool<ICompzeSqliteConnection, SqliteCommand>
{
   static ISqliteConnectionPool CreateInstance(string connectionString) => CreateInstance(() => connectionString);
   static SqliteConnectionPool CreateInstance(Func<string> getConnectionString) => new(getConnectionString);

   public class SqliteConnectionPool : ISqliteConnectionPool
   {
      readonly LazyCE<IDbConnectionPool<ICompzeSqliteConnection, SqliteCommand>> _pool;

      public SqliteConnectionPool(Func<string> getConnectionString)
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
