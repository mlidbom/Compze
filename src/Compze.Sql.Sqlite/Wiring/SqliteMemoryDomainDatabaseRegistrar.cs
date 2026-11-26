using Compze.DependencyInjection.Abstractions;
using Compze.Sql.Sqlite.Wiring._internal;

namespace Compze.Sql.Sqlite.Wiring;

public static class SqliteMemoryDomainDatabaseRegistrar
{
   ///<summary>Declares the domain database this endpoint joins: a process-local in-memory sqlite database named<br/>
   /// <paramref name="databaseName"/> — transient storage that lives and dies with the process, reached without touching disk.<br/>
   /// The in-memory sibling of <see cref="SqliteDomainDatabaseRegistrar.SqliteDomainDatabase(IComponentRegistrar, string)"/>: the connection pool every<br/>
   /// sql layer the endpoint registers stores its data through, each contributing its own schema, created before the<br/>
   /// database's first use.</summary>
   ///<remarks>Composable under the testing hosts today, where the database is drawn from the pooled test databases — the<br/>
   /// fastest backend for a consumer's own specifications. A production composition refuses the declaration for now: whether<br/>
   /// in-memory sqlite should also serve as production transient storage is an open design question.</remarks>
   ///<remarks>A transaction takes the database's connection as its one durable resource, whose commit is the transaction's decision: the<br/>
   /// database commits once every other participant of the transaction has prepared, and before any is told the outcome. So a<br/>
   /// transaction can't also hold another durable resource, such as another database's connection or a resource manager of your own.<br/>
   /// When that takes part first, the write to this database throws, saying so. When it takes part second, .NET refuses it itself:<br/>
   /// "Implicit distributed transactions have not been enabled".<br/>
   /// With distributed transactions enabled, the connection refuses to prepare a two-phase commit, and the transaction rolls back.<br/>
   /// Keep each database, and each other durable resource, in transactions of its own.</remarks>
   public static IComponentRegistrar SqliteMemoryDomainDatabase(this IComponentRegistrar registrar, string databaseName) =>
      registrar.SqliteMemoryConnectionPool(databaseName);
}
