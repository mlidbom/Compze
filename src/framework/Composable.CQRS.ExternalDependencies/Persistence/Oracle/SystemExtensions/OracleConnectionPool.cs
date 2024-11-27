using System;
using System.Threading.Tasks;
using Composable.Persistence.Common.AdoCE;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.ThreadingCE.TasksCE;
using Oracle.ManagedDataAccess.Client;

namespace Composable.Persistence.Oracle.SystemExtensions;

interface IOracleConnectionPool : IDbConnectionPool<IComposableOracleConnection, OracleCommand>
{
   static IOracleConnectionPool CreateInstance(string connectionString) => CreateInstance(() => connectionString);
   static OracleConnectionPool CreateInstance(Func<string> getConnectionString) => new(getConnectionString);

   class OracleConnectionPool : IOracleConnectionPool
   {
      readonly OptimizedLazy<IDbConnectionPool<IComposableOracleConnection, OracleCommand>> _pool;

      internal OracleConnectionPool(Func<string> getConnectionString)
      {
         _pool = new OptimizedLazy<IDbConnectionPool<IComposableOracleConnection, OracleCommand>>(
            () =>
            {
               var connectionString = getConnectionString();
               return DbConnectionManager<IComposableOracleConnection, OracleCommand>.ForConnectionString(
                  connectionString,
                  PoolableConnectionFlags.Defaults,
                  IComposableOracleConnection.Create);
            });
      }
      public override string ToString() => _pool.ValueIfInitialized()?.ToString() ?? "Not initialized";
      public TResult UseConnection<TResult>(Func<IComposableOracleConnection, TResult> func) => _pool.Value.UseConnection(func);
      public async Task<TResult> UseConnectionAsync<TResult>(Func<IComposableOracleConnection, Task<TResult>> func) => await _pool.Value.UseConnectionAsync(func).CaF();
   }
}