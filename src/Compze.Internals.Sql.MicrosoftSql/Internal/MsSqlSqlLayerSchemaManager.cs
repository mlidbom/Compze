using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Threading;
using Compze.Internals.Sql.MicrosoftSql.Private;

namespace Compze.Internals.Sql.MicrosoftSql.Internal;

///<summary>Runs the supplied schema-creation scripts against its database as a single suppressed-transaction batch — exactly once, on first touch.</summary>
///<remarks>For the domain database an endpoint joins the scripts arrive as <see cref="MsSqlSchemaContribution"/>s, each feature backend's registration<br/>
/// contributing its own — so neither this plumbing nor any composing layer references or enumerates the feature backends.<br/>
/// Creating all tables together up front (before any business transaction takes a lock) is what keeps schema creation off the<br/>
/// hot path of a write/read-locked transaction.</remarks>
class MsSqlSqlLayerSchemaManager
{
   //Several endpoints joining one domain database create their schemas concurrently - from one process or many - and the
   //scripts' IF-NOT-EXISTS guards are not concurrency-safe DDL. The engine's application lock serializes schema creation
   //across connections and processes; acquisition, batch, and release must share one connection, because the lock is
   //session-scoped.
   const string AcquireSchemaCreationLockSql =
      """
      DECLARE @lockResult int;
      EXEC @lockResult = sp_getapplock @Resource = 'Compze.SchemaCreation', @LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = 60000;
      IF @lockResult < 0 THROW 51000, 'Failed to acquire the schema-creation application lock (Compze.SchemaCreation) within 60 seconds.', 1;
      """;

   const string ReleaseSchemaCreationLockSql = "EXEC sp_releaseapplock @Resource = 'Compze.SchemaCreation', @LockOwner = 'Session';";

   readonly IMsSqlConnectionPool _connectionPool;
   readonly string _schemaCreationSql;
   readonly RunOnceAsync _runOnce = new();

   public MsSqlSqlLayerSchemaManager(IMsSqlConnectionPool connectionPool, IReadOnlyList<string> schemaCreationScripts)
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
