using System;
using System.Threading.Tasks;
using Compze.Persistence.Common.AdoCE;
using Compze.SystemCE;
using Compze.SystemCE.ThreadingCE.TasksCE;
using Npgsql;

namespace Compze.Persistence.PgSql.SystemExtensions;

interface IPgSqlConnectionPool : IDbConnectionPool<IComposableNpgsqlConnection, NpgsqlCommand>
{
   internal static IPgSqlConnectionPool CreateInstance1(Func<string> getConnectionString) => new PgSqlConnectionPool(getConnectionString);
   internal static IPgSqlConnectionPool CreateInstance(string connectionString) => new PgSqlConnectionPool(connectionString);

   class PgSqlConnectionPool : IPgSqlConnectionPool
   {
      readonly OptimizedLazy<IDbConnectionPool<IComposableNpgsqlConnection, NpgsqlCommand>> _pool;

      public PgSqlConnectionPool(string connectionString) : this(() => connectionString) {}

      public PgSqlConnectionPool(Func<string> getConnectionString)
      {
         _pool = new OptimizedLazy<IDbConnectionPool<IComposableNpgsqlConnection, NpgsqlCommand>>(
            () =>
            {
               var connectionString = getConnectionString();
               return DbConnectionManager<IComposableNpgsqlConnection, NpgsqlCommand>.ForConnectionString(
                  connectionString,
                  PoolableConnectionFlags.MustUseSameConnectionThroughoutATransaction,
                  IComposableNpgsqlConnection.Create);
            });
      }

      public TResult UseConnection<TResult>(Func<IComposableNpgsqlConnection, TResult> func) => _pool.Value.UseConnection(func);
      public async Task<TResult> UseConnectionAsync<TResult>(Func<IComposableNpgsqlConnection, Task<TResult>> func) => await _pool.Value.UseConnectionAsync(func).CaF();

      public override string ToString() => _pool.ValueIfInitialized()?.ToString() ?? "Not initialized";
   }
}
