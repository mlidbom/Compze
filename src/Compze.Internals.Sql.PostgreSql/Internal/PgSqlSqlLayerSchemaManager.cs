using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Threading;
using Compze.Internals.Sql.PostgreSql.Private;

namespace Compze.Internals.Sql.PostgreSql.Internal;

///<summary>Runs the supplied schema-creation scripts against its database as a single suppressed-transaction batch — exactly once, on first touch.</summary>
///<remarks>For the domain database an endpoint joins the scripts arrive as <see cref="PgSqlSchemaContribution"/>s, each feature backend's registration<br/>
/// contributing its own — so neither this plumbing nor any composing layer references or enumerates the feature backends.<br/>
/// Creating all tables together up front (before any business transaction takes a lock) is what keeps schema creation off the<br/>
/// hot path of a write/read-locked transaction.</remarks>
class PgSqlSqlLayerSchemaManager
{
   //Several endpoints joining one domain database create their schemas concurrently - from one process or many - and the
   //scripts' IF-NOT-EXISTS guards are not concurrency-safe DDL. The engine's advisory lock serializes schema creation
   //across connections and processes; acquisition, batch, and release must share one connection, because the lock is
   //session-scoped.
   //The key is scoped to the CURRENT database (current_database()): a PostgreSQL advisory lock is CLUSTER-GLOBAL by key, so a
   //constant key would serialize schema creation across every unrelated database in the cluster - a needless cross-database
   //bottleneck and a latent deadlock risk. Per-database scoping serializes only the endpoints that actually share one domain database.
   const string AcquireSchemaCreationLockSql = "SELECT pg_advisory_lock(hashtext('Compze.SchemaCreation:' || current_database())::bigint);";
   const string ReleaseSchemaCreationLockSql = "SELECT pg_advisory_unlock(hashtext('Compze.SchemaCreation:' || current_database())::bigint);";

   readonly IPgSqlConnectionPool _connectionPool;
   readonly string _schemaCreationSql;
   readonly RunOnceAsync _runOnce = new();

   public PgSqlSqlLayerSchemaManager(IPgSqlConnectionPool connectionPool, IReadOnlyList<string> schemaCreationScripts)
   {
      _connectionPool = connectionPool;
      _schemaCreationSql = string.Join($"{Environment.NewLine}{Environment.NewLine}", schemaCreationScripts);
   }

   public async Task EnsureSchemaInitializedAsync() => await _runOnce.RunIfFirstCallAsync(async () =>
      await TransactionScopeCe.SuppressAmbientAsync(async () =>
         await _connectionPool.UseConnectionAsync(async connection =>
         {
            await connection.ExecuteNonQueryAsync(AcquireSchemaCreationLockSql).caf();
            try
            {
               return await connection.ExecuteNonQueryAsync(_schemaCreationSql).caf();
            }
            finally
            {
               await connection.ExecuteNonQueryAsync(ReleaseSchemaCreationLockSql).caf();
            }
         }).caf()).caf()).caf();

   public void EnsureSchemaInitialized() => EnsureSchemaInitializedAsync().Wait();
}
