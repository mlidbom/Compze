using System;
using System.Threading.Tasks;
using Compze.Persistence.Common.AdoCE;
using Compze.SystemCE;
using Compze.SystemCE.ThreadingCE.TasksCE;
using Microsoft.Data.SqlClient;

namespace Compze.Persistence.MsSql.SystemExtensions;

interface IMsSqlConnectionPool : IDbConnectionPool<ICompzeMsSqlConnection, SqlCommand>
{
   static IMsSqlConnectionPool CreateInstance(string connectionString) => CreateInstance(() => connectionString);
   static MsSqlConnectionPool CreateInstance(Func<string> getConnectionString) => new(getConnectionString);

   class MsSqlConnectionPool : IMsSqlConnectionPool
   {
      readonly OptimizedLazy<IDbConnectionPool<ICompzeMsSqlConnection, SqlCommand>> _pool;

      public MsSqlConnectionPool(Func<string> getConnectionString)
      {
         _pool = new OptimizedLazy<IDbConnectionPool<ICompzeMsSqlConnection, SqlCommand>>(
            () =>
            {
               var connectionString = getConnectionString();
               return DbConnectionManager<ICompzeMsSqlConnection, SqlCommand>.ForConnectionString(
                  connectionString,
                  PoolableConnectionFlags.Defaults,
                  ICompzeMsSqlConnection.Create);
            });
      }

      public override string ToString() => _pool.ValueIfInitialized()?.ToString() ?? "Not initialized";
      public TResult UseConnection<TResult>(Func<ICompzeMsSqlConnection, TResult> func) => _pool.Value.UseConnection(func);
      public async Task<TResult> UseConnectionAsync<TResult>(Func<ICompzeMsSqlConnection, Task<TResult>> func) => await _pool.Value.UseConnectionAsync(func).CaF();
   }
}
