using System;
using System.Threading.Tasks;
using Composable.Persistence.Common.AdoCE;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE;
using MySql.Data.MySqlClient;

namespace Composable.Persistence.MySql.SystemExtensions;

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

      public Task<TResult> UseConnectionAsyncFlex<TResult>(SyncOrAsync syncOrAsync, Func<IComposableMySqlConnection, Task<TResult>> func)
         => _pool.Value.UseConnectionAsyncFlex(syncOrAsync, func);

      public override string ToString() => _pool.ValueIfInitialized()?.ToString() ?? "Not initialized";
   }
}