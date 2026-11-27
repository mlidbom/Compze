using Compze.Sql.Common._internal;
using Compze.Sql.Common._internal.Abstractions;
using Compze.Threading;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Npgsql;

namespace Compze.Sql.PostgreSql._internal;

interface IPgSqlConnectionPool : IDbConnectionPool<ICompzeNpgsqlConnection, NpgsqlCommand>
{
   public static IPgSqlConnectionPool CreateInstance1(Func<string> getConnectionString) => new PgSqlConnectionPool(getConnectionString);
   public static IPgSqlConnectionPool CreateInstance(string connectionString) => new PgSqlConnectionPool(connectionString);

   ///<summary>The connection string identifying the database this pool serves — what a caller needs to open a dedicated,<br/>
   /// long-held session against the same database, outside the pool's fresh-connection-per-operation model (the endpoint<br/>
   /// catalog's process lock is such a session). It is the <see cref="CompzePgSqlConnectionString"/>, so such a session<br/>
   /// gets the settings the pool's own connections have.</summary>
   string ConnectionString { get; }

   ///<summary>A pool of its own on this pool's database, of at most <paramref name="maxPoolSize"/> connections, which the server shows as<br/>
   /// <paramref name="applicationName"/>'s: for operations that commit on their own and may run inside a unit of work, which no connection<br/>
   /// held by a unit of work may keep waiting. See <see cref="PoolsOfTheirOwn"/>.</summary>
   IPgSqlConnectionPool APoolOfItsOwn(string applicationName, int maxPoolSize) =>
      CreateInstance1(() => PoolsOfTheirOwn.Record(ConnectionString,
                                                    new NpgsqlConnectionStringBuilder(ConnectionString) { ApplicationName = applicationName, MinPoolSize = 0, MaxPoolSize = maxPoolSize }.ConnectionString));

   public class PgSqlConnectionPool : IPgSqlConnectionPool
   {
      readonly LazyCE<IDbConnectionPool<ICompzeNpgsqlConnection, NpgsqlCommand>> _pool;
      readonly Func<string> _getConnectionString;

      internal PgSqlConnectionPool(string connectionString) : this(() => connectionString) {}

      internal PgSqlConnectionPool(Func<string> getConfiguredConnectionString)
      {
         var getConnectionString = () => CompzePgSqlConnectionString.From(getConfiguredConnectionString());
         _getConnectionString = getConnectionString;
         _pool = new LazyCE<IDbConnectionPool<ICompzeNpgsqlConnection, NpgsqlCommand>>(
            () =>
            {
               var connectionString = getConnectionString();
               return DbConnectionPool<ICompzeNpgsqlConnection, NpgsqlCommand>.ForConnectionString(
                  connectionString,
                  ICompzeNpgsqlConnection.Create);
            });
      }

      public string ConnectionString => _getConnectionString();

      public TResult UseConnection<TResult>(Func<ICompzeNpgsqlConnection, TResult> func) => _pool.Value.UseConnection(func);
      public async Task<TResult> UseConnectionAsync<TResult>(Func<ICompzeNpgsqlConnection, Task<TResult>> func) => await _pool.Value.UseConnectionAsync(func).caf();

      public override string ToString() => _pool.ValueIfInitialized()?.ToString() ?? "Not initialized";
   }
}
