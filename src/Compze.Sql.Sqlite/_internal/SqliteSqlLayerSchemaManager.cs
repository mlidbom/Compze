using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
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
   readonly RunOnceAsync _runOnce = new();

   public SqliteSqlLayerSchemaManager(ISqliteConnectionPool connectionPool, IReadOnlyList<string> schemaCreationScripts)
   {
      _connectionPool = connectionPool;
      _schemaCreationSql = string.Join($"{Environment.NewLine}{Environment.NewLine}", schemaCreationScripts);
   }

   public async Task EnsureSchemaInitializedAsync() => await _runOnce.RunIfFirstCallAsync(async () =>
      await TransactionScopeCe.SuppressAmbientAsync(async () =>
         await _connectionPool.ExecuteNonQueryAsync(_schemaCreationSql).caf()).caf()).caf();

   ///<summary>The synchronous form of <see cref="EnsureSchemaInitializedAsync"/>, for synchronous SQL layers.</summary>
   ///<remarks>bug: it blocks the calling thread on the asynchronous form, whose continuations need thread-pool threads: at the first call,<br/>
   /// with every pool thread busy, as under many units of work at once, it waits until one comes free. Found 2026-11-24, by the memory<br/>
   /// image state store's specification of two memory images adding cards to one deck from many threads at once.</remarks>
   public void EnsureSchemaInitialized() => EnsureSchemaInitializedAsync().Wait();
}
