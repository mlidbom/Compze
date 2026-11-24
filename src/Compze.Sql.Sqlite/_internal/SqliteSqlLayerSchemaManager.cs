using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Threading;
using Compze.Sql.Sqlite._private;

namespace Compze.Sql.Sqlite._internal;

///<summary>Runs the supplied schema-creation scripts against its database as a single suppressed-transaction batch — exactly once, on first touch.</summary>
///<remarks>For the domain database an endpoint joins the scripts arrive as <see cref="SqliteSchemaContribution"/>s, each feature backend's registration<br/>
/// contributing its own — so neither this plumbing nor any composing layer references or enumerates the feature backends.<br/>
/// Creating all tables together up front (before any business transaction takes a lock) is what keeps schema creation off the<br/>
/// hot path of a write/read-locked transaction.</remarks>
class SqliteSqlLayerSchemaManager
{
   readonly ISqliteConnectionPool _connectionPool;
   readonly string _schemaCreationSql;
   readonly RunOnce _runOnce = new();

   public SqliteSqlLayerSchemaManager(ISqliteConnectionPool connectionPool, IReadOnlyList<string> schemaCreationScripts)
   {
      _connectionPool = connectionPool;
      _schemaCreationSql = string.Join($"{Environment.NewLine}{Environment.NewLine}", schemaCreationScripts);
   }

   ///<summary>Creates the schema if this is the first call, or waits until the first call has, blocking the calling thread either way.</summary>
   ///<remarks>Synchronous for every caller, the asynchronous SQL layers' too, which wait for it only once, as the database is first used:<br/>
   /// a call waiting for another's creation then blocks on a signal that the creating thread gives, and neither needs a free thread-pool<br/>
   /// thread. Blocking on an asynchronous creation instead needed one for each of its continuations, while many units of work at once<br/>
   /// could hold every pool thread, each blocked waiting for the creation.</remarks>
   ///<remarks>todo: (Magnus, 2026-11-24) Restore asynchronous creation for the asynchronous callers, the Tessaging SQL layers, whose<br/>
   /// <c>InitAsync</c> now blocks its thread on this once. A synchronous caller must still never block on a creation that runs<br/>
   /// asynchronously: it would need a free thread-pool thread for each of the creation's continuations, the starvation described<br/>
   /// above. That needs a run-once in Compze.Threading that serves both kinds of caller, which neither <see cref="RunOnce"/> nor<br/>
   /// <see cref="RunOnceAsync"/> does.</remarks>
   public void EnsureSchemaInitialized() => _runOnce.RunIfFirstCall(() => TransactionScopeCe.SuppressAmbient(() =>
      _connectionPool.ExecuteNonQuery(_schemaCreationSql)));
}
