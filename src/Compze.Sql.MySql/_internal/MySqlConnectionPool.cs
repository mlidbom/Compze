using Compze.Sql.Common._internal;
using Compze.Sql.Common._internal.Abstractions;
using Compze.Threading;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using MySqlConnector;

namespace Compze.Sql.MySql._internal;

interface IMySqlConnectionPool : IDbConnectionPool<ICompzeMySqlConnection, MySqlCommand>
{
   static IMySqlConnectionPool CreateInstance(string connectionString) => CreateInstance(() => connectionString);
   static MySqlConnectionPool CreateInstance(Func<string> getConnectionString) => new(getConnectionString);

   ///<summary>The connection string identifying the database this pool serves — what a caller needs to open a dedicated,<br/>
   /// long-held session against the same database, outside the pool's fresh-connection-per-operation model (the endpoint<br/>
   /// catalog's process lock is such a session). It is the <see cref="CompzeMySqlConnectionString"/>, so such a session<br/>
   /// gets the settings the pool's own connections have.</summary>
   string ConnectionString { get; }

   ///<summary>A pool of its own on this pool's database, of at most <paramref name="maxPoolSize"/> connections, which the server shows as<br/>
   /// <paramref name="applicationName"/>'s: for operations that commit on their own and may run inside a unit of work, which no connection<br/>
   /// held by a unit of work may keep waiting. See <see cref="PoolsOfTheirOwn"/>.</summary>
   IMySqlConnectionPool APoolOfItsOwn(string applicationName, int maxPoolSize) =>
      CreateInstance(() => PoolsOfTheirOwn.Record(ConnectionString,
                                                   new MySqlConnectionStringBuilder(ConnectionString) { ApplicationName = applicationName, MinimumPoolSize = 0, MaximumPoolSize = (uint)maxPoolSize }.ConnectionString));

   public class MySqlConnectionPool : IMySqlConnectionPool
   {
      readonly LazyCE<IDbConnectionPool<ICompzeMySqlConnection, MySqlCommand>> _pool;
      readonly Func<string> _getConnectionString;

      internal MySqlConnectionPool(Func<string> getConfiguredConnectionString)
      {
         var getConnectionString = () => CompzeMySqlConnectionString.From(getConfiguredConnectionString());
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
