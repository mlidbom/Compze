using System;
using System.Threading.Tasks;
using Compze.Persistence.Common.AdoCE;
using Compze.SystemCE;
using Compze.SystemCE.ThreadingCE.TasksCE;
using MySql.Data.MySqlClient;

namespace Compze.Persistence.MySql.SystemExtensions;

interface IMySqlConnectionPool : IDbConnectionPool<IComposableMySqlConnection, MySqlCommand>
{
   static IMySqlConnectionPool CreateInstance(string connectionString) => CreateInstance(() => connectionString);
   static MySqlConnectionPool CreateInstance(Func<string> getConnectionString) => new(getConnectionString);

   class MySqlConnectionPool : IMySqlConnectionPool
   {
      readonly OptimizedLazy<IDbConnectionPool<IComposableMySqlConnection, MySqlCommand>> _pool;

      public MySqlConnectionPool(Func<string> getConnectionString)
      {
         _pool = new OptimizedLazy<IDbConnectionPool<IComposableMySqlConnection, MySqlCommand>>(
            () =>
            {
               var connectionString = getConnectionString();
               return DbConnectionManager<IComposableMySqlConnection, MySqlCommand>.ForConnectionString(
                  connectionString,
                  PoolableConnectionFlags.Defaults,
                  IComposableMySqlConnection.Create);
            });
      }

      public override string ToString() => _pool.ValueIfInitialized()?.ToString() ?? "Not initialized";
      public TResult UseConnection<TResult>(Func<IComposableMySqlConnection, TResult> func) => _pool.Value.UseConnection(func);
      public async Task<TResult> UseConnectionAsync<TResult>(Func<IComposableMySqlConnection, Task<TResult>> func) => await _pool.Value.UseConnectionAsync(func).CaF();
   }
}