using Compze.DependencyInjection.Abstractions;
using Compze.Sql.Sqlite.Wiring._internal;

namespace Compze.Sql.Sqlite.Wiring;

public static class SqliteMemoryDomainDatabaseRegistrar
{
   ///<summary>Declares the domain database this endpoint joins: a process-local in-memory sqlite database named<br/>
   /// <paramref name="databaseName"/> — transient storage that lives and dies with the process, reached without touching disk.<br/>
   /// The in-memory sibling of <see cref="SqliteDomainDatabaseRegistrar.SqliteDomainDatabase"/>: the connection pool every<br/>
   /// sql layer the endpoint registers stores its data through, each contributing its own schema, created before the<br/>
   /// database's first use.</summary>
   ///<remarks>Composable under the testing hosts today, where the database is drawn from the pooled test databases — the<br/>
   /// fastest backend for a consumer's own specifications. A production composition refuses the declaration for now: whether<br/>
   /// in-memory sqlite should also serve as production transient storage is an open design question.</remarks>
   public static IComponentRegistrar SqliteMemoryDomainDatabase(this IComponentRegistrar registrar, string databaseName) =>
      registrar.SqliteMemoryConnectionPool(databaseName);
}
