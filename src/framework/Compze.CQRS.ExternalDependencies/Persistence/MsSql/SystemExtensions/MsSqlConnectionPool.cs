using System;
using System.Threading.Tasks;
using Compze.Persistence.Common.AdoCE;
using Compze.SystemCE;
using Compze.SystemCE.ThreadingCE.TasksCE;
using Microsoft.Data.SqlClient;

namespace Compze.Persistence.MsSql.SystemExtensions;

interface IMsSqlConnectionPool : IDbConnectionPool<IComposableMsSqlConnection, SqlCommand>
{
   static IMsSqlConnectionPool CreateInstance(string connectionString) => CreateInstance(() => connectionString);
   static MsSqlConnectionPool CreateInstance(Func<string> getConnectionString) => new(getConnectionString);

   class MsSqlConnectionPool : IMsSqlConnectionPool
   {
      readonly OptimizedLazy<IDbConnectionPool<IComposableMsSqlConnection, SqlCommand>> _pool;

      public MsSqlConnectionPool(Func<string> getConnectionString)
      {
         _pool = new OptimizedLazy<IDbConnectionPool<IComposableMsSqlConnection, SqlCommand>>(
            () =>
            {
               var connectionString = getConnectionString();
               return DbConnectionManager<IComposableMsSqlConnection, SqlCommand>.ForConnectionString(
                  connectionString,
                  PoolableConnectionFlags.Defaults,
                  IComposableMsSqlConnection.Create);
            });
      }

      public override string ToString() => _pool.ValueIfInitialized()?.ToString() ?? "Not initialized";
      public TResult UseConnection<TResult>(Func<IComposableMsSqlConnection, TResult> func) => _pool.Value.UseConnection(func);
      public async Task<TResult> UseConnectionAsync<TResult>(Func<IComposableMsSqlConnection, Task<TResult>> func) => await _pool.Value.UseConnectionAsync(func).CaF();
   }
}
