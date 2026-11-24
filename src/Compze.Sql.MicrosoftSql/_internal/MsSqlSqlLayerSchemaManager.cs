using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Threading;
using Compze.Sql.MicrosoftSql._private;

namespace Compze.Sql.MicrosoftSql._internal;

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
   readonly RunOnce _runOnce = new();

   public MsSqlSqlLayerSchemaManager(IMsSqlConnectionPool connectionPool, IReadOnlyList<string> schemaCreationScripts)
   {
      _connectionPool = connectionPool;
      _schemaCreationSql = string.Join($"{Environment.NewLine}{Environment.NewLine}", schemaCreationScripts);
   }

   ///<summary>Creates the schema if this is the first call, or waits until the first call has, blocking the calling thread either way.</summary>
   ///<remarks>Synchronous for every caller, the asynchronous SQL layers' too, which wait for it only once, as the database is first used:<br/>
   /// a call waiting for another's creation then blocks on a signal that the creating thread gives, and neither needs a free thread-pool<br/>
   /// thread. Blocking on an asynchronous creation instead needed one for each of its continuations, while many units of work at once<br/>
   /// could hold every pool thread, each blocked waiting for the creation.</remarks>
   public void EnsureSchemaInitialized() => _runOnce.RunIfFirstCall(() => TransactionScopeCe.SuppressAmbient(() =>
      _connectionPool.UseConnection(connection =>
      {
         connection.ExecuteNonQuery(AcquireSchemaCreationLockSql);
         try
         {
            connection.ExecuteNonQuery(_schemaCreationSql);
         }
         finally
         {
            connection.ExecuteNonQuery(ReleaseSchemaCreationLockSql);
         }
      })));
}
