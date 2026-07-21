using Compze.Sql.Common._internal;
using Compze.Sql.Common._internal.Abstractions;
using Compze.Internals.SystemCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Microsoft.Data.SqlClient;

namespace Compze.Sql.MicrosoftSql._internal;

interface IMsSqlConnectionPool : IDbConnectionPool<ICompzeMsSqlConnection, SqlCommand>
{
   static IMsSqlConnectionPool CreateInstance(string connectionString) => CreateInstance(() => connectionString);
   static MsSqlConnectionPool CreateInstance(Func<string> getConnectionString) => new(getConnectionString);

   ///<summary>The connection string identifying the database this pool serves — what a caller needs to open a dedicated,<br/>
   /// long-held session against the same database, outside the pool's fresh-connection-per-operation model (the endpoint<br/>
   /// catalog's process lock is such a session).</summary>
   string ConnectionString { get; }

   public class MsSqlConnectionPool : IMsSqlConnectionPool
   {
      readonly LazyCE<IDbConnectionPool<ICompzeMsSqlConnection, SqlCommand>> _pool;
      readonly Func<string> _getConnectionString;

      internal MsSqlConnectionPool(Func<string> getConnectionString)
      {
         _getConnectionString = getConnectionString;
         _pool = new LazyCE<IDbConnectionPool<ICompzeMsSqlConnection, SqlCommand>>(
            () =>
            {
               var connectionString = getConnectionString();
               return DbConnectionPool<ICompzeMsSqlConnection, SqlCommand>.ForConnectionString(
                  connectionString,
                  ICompzeMsSqlConnection.Create);
            });
      }

      public string ConnectionString => _getConnectionString();

      public override string ToString() => _pool.ValueIfInitialized()?.ToString() ?? "Not initialized";
      public TResult UseConnection<TResult>(Func<ICompzeMsSqlConnection, TResult> func) => _pool.Value.UseConnection(func);
      public async Task<TResult> UseConnectionAsync<TResult>(Func<ICompzeMsSqlConnection, Task<TResult>> func) => await _pool.Value.UseConnectionAsync(func).caf();
   }
}
