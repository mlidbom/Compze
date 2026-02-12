using System;
using System.Threading.Tasks;
using Compze.Sql.Common;
using Compze.Sql.Common.Abstractions;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;
using Microsoft.Data.SqlClient;

namespace Compze.Sql.MicrosoftSql;

public interface IMsSqlConnectionPool : IDbConnectionPool<ICompzeMsSqlConnection, SqlCommand>
{
   static IMsSqlConnectionPool CreateInstance(string connectionString) => CreateInstance(() => connectionString);
   static MsSqlConnectionPool CreateInstance(Func<string> getConnectionString) => new(getConnectionString);

   public class MsSqlConnectionPool : IMsSqlConnectionPool
   {
      readonly LazyCE<IDbConnectionPool<ICompzeMsSqlConnection, SqlCommand>> _pool;

      public MsSqlConnectionPool(Func<string> getConnectionString)
      {
         _pool = new LazyCE<IDbConnectionPool<ICompzeMsSqlConnection, SqlCommand>>(
            () =>
            {
               var connectionString = getConnectionString();
               return DbConnectionPool<ICompzeMsSqlConnection, SqlCommand>.ForConnectionString(
                  connectionString,
                  PoolableConnectionFlags.Defaults,
                  ICompzeMsSqlConnection.Create);
            });
      }

      public override string ToString() => _pool.ValueIfInitialized()?.ToString() ?? "Not initialized";
      public TResult UseConnection<TResult>(Func<ICompzeMsSqlConnection, TResult> func) => _pool.Value.UseConnection(func);
      public async Task<TResult> UseConnectionAsync<TResult>(Func<ICompzeMsSqlConnection, Task<TResult>> func) => await _pool.Value.UseConnectionAsync(func).caf();
   }
}
