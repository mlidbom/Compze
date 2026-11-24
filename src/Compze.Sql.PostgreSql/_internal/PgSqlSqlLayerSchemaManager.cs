using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Threading;
using Compze.Sql.PostgreSql._private;

namespace Compze.Sql.PostgreSql._internal;

///<summary>Runs the supplied schema-creation statements against its database, outside any transaction — exactly once, on first touch.</summary>
///<remarks>For the domain database an endpoint joins the statements arrive as <see cref="PgSqlSchemaContribution"/>s, each feature backend's registration<br/>
/// contributing its own — so neither this plumbing nor any composing layer references or enumerates the feature backends.<br/>
/// Creating all tables together up front (before any business transaction takes a lock) is what keeps schema creation off the<br/>
/// hot path of a write/read-locked transaction.</remarks>
///<remarks>Each statement runs as a command of its own: Npgsql 9 sends the second and later statements of a synchronous command, and<br/>
/// flushes the whole command, from the thread pool, while the calling thread waits for the answer. With every pool thread busy, as<br/>
/// under many units of work at the database's first use, the server receives nothing until the pool adds a thread.<br/>
/// So each statement commits by itself, and a creation that fails part way leaves the statements before it done: every statement<br/>
/// may run more than once, as each endpoint and each process joining the database runs them all.</remarks>
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
   readonly IReadOnlyList<string> _schemaCreationStatements;
   readonly RunOnce _runOnce = new();

   public PgSqlSqlLayerSchemaManager(IPgSqlConnectionPool connectionPool, IReadOnlyList<string> schemaCreationStatements)
   {
      _connectionPool = connectionPool;
      _schemaCreationStatements = schemaCreationStatements;
   }

   ///<summary>Creates the schema if this is the first call, or waits until the first call has, blocking the calling thread either way.</summary>
   ///<remarks>Synchronous for every caller, the asynchronous SQL layers' too, which wait for it only once, as the database is first used:<br/>
   /// a call waiting for another's creation then blocks on a signal that the creating thread gives, and neither needs a free thread-pool<br/>
   /// thread. Blocking on an asynchronous creation instead needed one for each of its continuations, while many units of work at once<br/>
   /// could hold every pool thread, each blocked waiting for the creation.</remarks>
   public void EnsureSchemaInitialized()
   {
      //todo: (Magnus, 2026-11-24) Restore asynchronous creation for the asynchronous callers, the Tessaging SQL layers, whose
      //   InitAsync now blocks its thread on this once. A synchronous caller must still never block on a creation that runs
      //   asynchronously: it would need a free thread-pool thread for each of the creation's continuations, the starvation described
      //   above. That needs a run-once in Compze.Threading that serves both kinds of caller, which neither RunOnce nor
      //   RunOnceAsync does.
      _runOnce.RunIfFirstCall(() => TransactionScopeCe.SuppressAmbient(() =>
         _connectionPool.UseConnection(connection =>
         {
            connection.ExecuteNonQuery(AcquireSchemaCreationLockSql);
            try
            {
               foreach(var statement in _schemaCreationStatements) connection.ExecuteNonQuery(statement);
            }
            finally
            {
               connection.ExecuteNonQuery(ReleaseSchemaCreationLockSql);
            }
         })));
   }
}
