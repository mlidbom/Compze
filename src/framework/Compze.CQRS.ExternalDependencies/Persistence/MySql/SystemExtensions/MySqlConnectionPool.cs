using System;
using System.Threading.Tasks;
using Compze.Persistence.Common.AdoCE;
using Compze.SystemCE;
using Compze.SystemCE.ThreadingCE.TasksCE;
using MySql.Data.MySqlClient;

namespace Compze.Persistence.MySql.SystemExtensions;

interface IMySqlConnectionPool : IDbConnectionPool<ICompzeMySqlConnection, MySqlCommand>
{
   static IMySqlConnectionPool CreateInstance(string connectionString) => CreateInstance(() => connectionString);
   static MySqlConnectionPool CreateInstance(Func<string> getConnectionString) => new(getConnectionString);

   class MySqlConnectionPool : IMySqlConnectionPool
   {
      readonly OptimizedLazy<IDbConnectionPool<ICompzeMySqlConnection, MySqlCommand>> _pool;

      public MySqlConnectionPool(Func<string> getConnectionString)
      {
         _pool = new OptimizedLazy<IDbConnectionPool<ICompzeMySqlConnection, MySqlCommand>>(
            () =>
            {
               var connectionString = getConnectionString();
               return DbConnectionManager<ICompzeMySqlConnection, MySqlCommand>.ForConnectionString(
                  connectionString,
                  PoolableConnectionFlags.Defaults,
                  ICompzeMySqlConnection.Create);
            });
      }

      public override string ToString() => _pool.ValueIfInitialized()?.ToString() ?? "Not initialized";
      public TResult UseConnection<TResult>(Func<ICompzeMySqlConnection, TResult> func) => _pool.Value.UseConnection(func);
      public async Task<TResult> UseConnectionAsync<TResult>(Func<ICompzeMySqlConnection, Task<TResult>> func) => await _pool.Value.UseConnectionAsync(func).CaF();
   }
}