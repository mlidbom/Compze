using System.Globalization;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Threading;
using Compze.Sql.MySql._private;

namespace Compze.Sql.MySql._internal;

///<summary>Runs the supplied schema-creation scripts against its database as a single suppressed-transaction batch — exactly once, on first touch.</summary>
///<remarks>For the domain database an endpoint joins the scripts arrive as <see cref="MySqlSchemaContribution"/>s, each feature backend's registration<br/>
/// contributing its own — so neither this plumbing nor any composing layer references or enumerates the feature backends.<br/>
/// Creating all tables together up front (before any business transaction takes a lock) is what keeps schema creation off the<br/>
/// hot path of a write/read-locked transaction.</remarks>
class MySqlSqlLayerSchemaManager
{
   //Several endpoints joining one domain database create their schemas concurrently - from one process or many - and the
   //scripts' IF-NOT-EXISTS guards are not concurrency-safe DDL. The engine's named lock serializes schema creation across
   //connections and processes; acquisition, batch, and release must share one connection, because the lock is
   //session-scoped.
   //The name is scoped to the CURRENT database (DATABASE()): a MySQL GET_LOCK is SERVER-GLOBAL by name, so a constant name
   //would serialize schema creation across every unrelated database on the same server - a needless cross-database bottleneck
   //and a latent deadlock risk. Per-database scoping serializes only the endpoints that actually share one domain database.
   const string SchemaCreationLockNamePrefix = "Compze.SchemaCreation";

   readonly IMySqlConnectionPool _connectionPool;
   readonly string _schemaCreationSql;
   readonly RunOnce _runOnce = new();

   public MySqlSqlLayerSchemaManager(IMySqlConnectionPool connectionPool, IReadOnlyList<string> schemaCreationScripts)
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
         //GET_LOCK returns 1 on success, 0 on timeout, NULL on error - anything but 1 must fail loud, or the batch
         //would run unserialized. DATABASE() scopes the lock to the connection's current database (see the prefix's remark).
         var lockResult = connection.ExecuteScalar($"SELECT GET_LOCK(CONCAT('{SchemaCreationLockNamePrefix}:', DATABASE()), 60)");
         if(Convert.ToInt64(lockResult ?? 0L, CultureInfo.InvariantCulture) != 1)
            throw new InvalidOperationException($"Failed to acquire the schema-creation lock ('{SchemaCreationLockNamePrefix}:<database>') within 60 seconds.");
         try
         {
            connection.ExecuteNonQuery(_schemaCreationSql);
         }
         finally
         {
            connection.ExecuteNonQuery($"SELECT RELEASE_LOCK(CONCAT('{SchemaCreationLockNamePrefix}:', DATABASE()))");
         }
      })));
}
