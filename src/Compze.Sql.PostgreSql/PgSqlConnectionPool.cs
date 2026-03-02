using Compze.Sql.Common;
using Compze.Sql.Common.Abstractions;
using Compze.Utilities.SystemCE;
using Npgsql;
using System;
using System.Threading.Tasks;
using Compze.Threading.TasksCE;

namespace Compze.Sql.PostgreSql;

public interface IPgSqlConnectionPool : IDbConnectionPool<ICompzeNpgsqlConnection, NpgsqlCommand>
{
   public static IPgSqlConnectionPool CreateInstance1(Func<string> getConnectionString) => new PgSqlConnectionPool(getConnectionString);
   public static IPgSqlConnectionPool CreateInstance(string connectionString) => new PgSqlConnectionPool(connectionString);

   public class PgSqlConnectionPool : IPgSqlConnectionPool
   {
      readonly LazyCE<IDbConnectionPool<ICompzeNpgsqlConnection, NpgsqlCommand>> _pool;

      internal PgSqlConnectionPool(string connectionString) : this(() => connectionString) {}

      internal PgSqlConnectionPool(Func<string> getConnectionString)
      {
         _pool = new LazyCE<IDbConnectionPool<ICompzeNpgsqlConnection, NpgsqlCommand>>(
            () =>
            {
               var connectionString = getConnectionString();
               return DbConnectionPool<ICompzeNpgsqlConnection, NpgsqlCommand>.ForConnectionString(
                  connectionString,
                  PoolableConnectionFlags.MustUseSameConnectionThroughoutATransaction,
                  ICompzeNpgsqlConnection.Create);
            });
      }

      public TResult UseConnection<TResult>(Func<ICompzeNpgsqlConnection, TResult> func) => _pool.Value.UseConnection(func);
      public async Task<TResult> UseConnectionAsync<TResult>(Func<ICompzeNpgsqlConnection, Task<TResult>> func) => await _pool.Value.UseConnectionAsync(func).caf();

      public override string ToString() => _pool.ValueIfInitialized()?.ToString() ?? "Not initialized";
   }
}
