using System.Globalization;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Threading;

namespace Compze.Internals.Sql.MySql.Private;

///<summary>Runs the supplied schema-creation scripts against its database as a single suppressed-transaction batch — exactly once, on first touch.</summary>
///<remarks>For the endpoint's database the scripts arrive as <see cref="MySqlSchemaContribution"/>s, each feature backend's registration<br/>
/// contributing its own — so neither this plumbing nor any composing layer references or enumerates the feature backends.<br/>
/// Creating all tables together up front (before any business transaction takes a lock) is what keeps schema creation off the<br/>
/// hot path of a write/read-locked transaction.</remarks>
class MySqlSqlLayerSchemaManager
{
   //Several endpoints joining one domain database create their schemas concurrently - from one process or many - and the
   //scripts' IF-NOT-EXISTS guards are not concurrency-safe DDL. The engine's named lock serializes schema creation across
   //connections and processes; acquisition, batch, and release must share one connection, because the lock is
   //session-scoped.
   const string SchemaCreationLockName = "Compze.SchemaCreation";

   readonly IMySqlConnectionPool _connectionPool;
   readonly string _schemaCreationSql;
   readonly RunOnceAsync _runOnce = new();

   public MySqlSqlLayerSchemaManager(IMySqlConnectionPool connectionPool, IReadOnlyList<string> schemaCreationScripts)
   {
      _connectionPool = connectionPool;
      _schemaCreationSql = string.Join($"{Environment.NewLine}{Environment.NewLine}", schemaCreationScripts);
   }

   public async Task EnsureSchemaInitializedAsync() => await _runOnce.RunIfFirstCallAsync(async () =>
      await TransactionScopeCe.SuppressAmbientAsync(async () =>
         await _connectionPool.UseConnectionAsync(async connection =>
         {
            //GET_LOCK returns 1 on success, 0 on timeout, NULL on error - anything but 1 must fail loud, or the batch
            //would run unserialized.
            var lockResult = await connection.ExecuteScalarAsync($"SELECT GET_LOCK('{SchemaCreationLockName}', 60)").caf();
            if(Convert.ToInt64(lockResult ?? 0L, CultureInfo.InvariantCulture) != 1)
               throw new InvalidOperationException($"Failed to acquire the schema-creation lock ('{SchemaCreationLockName}') within 60 seconds.");
            try
            {
               return await connection.ExecuteNonQueryAsync(_schemaCreationSql).caf();
            }
            finally
            {
               await connection.ExecuteNonQueryAsync($"SELECT RELEASE_LOCK('{SchemaCreationLockName}')").caf();
            }
         }).caf()).caf()).caf();

   public void EnsureSchemaInitialized() => EnsureSchemaInitializedAsync().Wait();
}
