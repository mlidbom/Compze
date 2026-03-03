using Compze.Sql.Common;
using Compze.Sql.Common.Abstractions;
using Compze.SystemCE.ThreadingCE.TasksCE;
using Compze.Utilities.SystemCE;
using MySql.Data.MySqlClient;

namespace Compze.Sql.MySql;

public interface IMySqlConnectionPool : IDbConnectionPool<ICompzeMySqlConnection, MySqlCommand>
{
   static IMySqlConnectionPool CreateInstance(string connectionString) => CreateInstance(() => connectionString);
   static MySqlConnectionPool CreateInstance(Func<string> getConnectionString) => new(getConnectionString);

   public class MySqlConnectionPool : IMySqlConnectionPool
   {
      readonly LazyCE<IDbConnectionPool<ICompzeMySqlConnection, MySqlCommand>> _pool;

      internal MySqlConnectionPool(Func<string> getConnectionString)
      {
         _pool = new LazyCE<IDbConnectionPool<ICompzeMySqlConnection, MySqlCommand>>(
            () =>
            {
               var connectionString = getConnectionString();
               return DbConnectionPool<ICompzeMySqlConnection, MySqlCommand>.ForConnectionString(
                  connectionString,
                  PoolableConnectionFlags.MustUseSameConnectionThroughoutATransaction,//todo: verify that this is needed. Enabling for now to see if it fixes CI issues.
                  ICompzeMySqlConnection.Create);
            });
      }

      public override string ToString() => _pool.ValueIfInitialized()?.ToString() ?? "Not initialized";
      public TResult UseConnection<TResult>(Func<ICompzeMySqlConnection, TResult> func) => _pool.Value.UseConnection(func);
      public async Task<TResult> UseConnectionAsync<TResult>(Func<ICompzeMySqlConnection, Task<TResult>> func) => await _pool.Value.UseConnectionAsync(func).caf();
   }
}
