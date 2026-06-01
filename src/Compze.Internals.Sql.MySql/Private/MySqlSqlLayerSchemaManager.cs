using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Threading;

namespace Compze.Internals.Sql.MySql.Private;

// Creates every configured feature backend's schema in a single suppressed-transaction batch on first touch.
// The scripts are supplied by the composition that knows which backends are present, so the plumbing never
// references a feature backend. Creating all tables together up front (before any business transaction takes a
// lock) is what keeps schema creation off the hot path of a write/read-locked transaction.
class MySqlSqlLayerSchemaManager
{
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
         await _connectionPool.ExecuteNonQueryAsync(_schemaCreationSql).caf()).caf()).caf();

   public void EnsureSchemaInitialized() => EnsureSchemaInitializedAsync().Wait();
}
