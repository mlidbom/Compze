using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Threading;
using Compze.Sql.PostgreSql._private;

namespace Compze.Sql.PostgreSql._internal;

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
   //PostgreSQL scopes an advisory lock to the database it is taken in: the same key taken in two databases of one cluster is
   //two locks. So this lock serializes only the endpoints that share one domain database, never unrelated databases in the
   //cluster (verified on PostgreSQL 18, 2026-11-12).
   //refactor: the database name (current_database()) in the key adds nothing, given that scoping; a constant key serializes
   //   exactly the same endpoints. Drop it with the next change to this package that ships anyway.
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
