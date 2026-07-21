using Compze.Sql.Common._internal;
using Compze.Sql.Common._internal.Abstractions;
using Compze.Internals.SystemCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using MySqlConnector;

namespace Compze.Sql.MySql._internal;

interface IMySqlConnectionPool : IDbConnectionPool<ICompzeMySqlConnection, MySqlCommand>
{
   static IMySqlConnectionPool CreateInstance(string connectionString) => CreateInstance(() => connectionString);
   static MySqlConnectionPool CreateInstance(Func<string> getConnectionString) => new(getConnectionString);

   ///<summary>The connection string identifying the database this pool serves — what a caller needs to open a dedicated,<br/>
   /// long-held session against the same database, outside the pool's fresh-connection-per-operation model (the endpoint<br/>
   /// catalog's process lock is such a session).</summary>
   string ConnectionString { get; }

   public class MySqlConnectionPool : IMySqlConnectionPool
   {
      readonly LazyCE<IDbConnectionPool<ICompzeMySqlConnection, MySqlCommand>> _pool;
      readonly Func<string> _getConnectionString;

      internal MySqlConnectionPool(Func<string> getConnectionString)
      {
         _getConnectionString = getConnectionString;
         _pool = new LazyCE<IDbConnectionPool<ICompzeMySqlConnection, MySqlCommand>>(
            () =>
            {
               var connectionString = getConnectionString();
               return DbConnectionPool<ICompzeMySqlConnection, MySqlCommand>.ForConnectionString(
                  connectionString,
                  ICompzeMySqlConnection.Create);
            });
      }

      public string ConnectionString => _getConnectionString();

      public override string ToString() => _pool.ValueIfInitialized()?.ToString() ?? "Not initialized";
      public TResult UseConnection<TResult>(Func<ICompzeMySqlConnection, TResult> func) => _pool.Value.UseConnection(func);
      public async Task<TResult> UseConnectionAsync<TResult>(Func<ICompzeMySqlConnection, Task<TResult>> func) => await _pool.Value.UseConnectionAsync(func).caf();
   }
}
