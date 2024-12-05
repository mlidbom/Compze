using System;
using System.Threading.Tasks;
using Compze.Persistence.Common.AdoCE;
using Compze.SystemCE;
using Compze.SystemCE.ThreadingCE.TasksCE;
using Npgsql;

namespace Compze.Persistence.PgSql.SystemExtensions;

interface IPgSqlConnectionPool : IDbConnectionPool<ICompzeNpgsqlConnection, NpgsqlCommand>
{
   internal static IPgSqlConnectionPool CreateInstance1(Func<string> getConnectionString) => new PgSqlConnectionPool(getConnectionString);
   internal static IPgSqlConnectionPool CreateInstance(string connectionString) => new PgSqlConnectionPool(connectionString);

   class PgSqlConnectionPool : IPgSqlConnectionPool
   {
      readonly OptimizedLazy<IDbConnectionPool<ICompzeNpgsqlConnection, NpgsqlCommand>> _pool;

      public PgSqlConnectionPool(string connectionString) : this(() => connectionString) {}

      public PgSqlConnectionPool(Func<string> getConnectionString)
      {
         _pool = new OptimizedLazy<IDbConnectionPool<ICompzeNpgsqlConnection, NpgsqlCommand>>(
            () =>
            {
               var connectionString = getConnectionString();
               return DbConnectionManager<ICompzeNpgsqlConnection, NpgsqlCommand>.ForConnectionString(
                  connectionString,
                  PoolableConnectionFlags.MustUseSameConnectionThroughoutATransaction,
                  ICompzeNpgsqlConnection.Create);
            });
      }

      public TResult UseConnection<TResult>(Func<ICompzeNpgsqlConnection, TResult> func) => _pool.Value.UseConnection(func);
      public async Task<TResult> UseConnectionAsync<TResult>(Func<ICompzeNpgsqlConnection, Task<TResult>> func) => await _pool.Value.UseConnectionAsync(func).CaF();

      public override string ToString() => _pool.ValueIfInitialized()?.ToString() ?? "Not initialized";
   }
}
