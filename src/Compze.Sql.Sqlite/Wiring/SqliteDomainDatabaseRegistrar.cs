using Compze.DependencyInjection.Abstractions;
using Compze.Sql.Sqlite.Wiring._internal;

namespace Compze.Sql.Sqlite.Wiring;

public static class SqliteDomainDatabaseRegistrar
{
   ///<summary>Declares the domain database this endpoint joins: the sqlite database reached through<br/>
   /// <paramref name="connectionStringName"/> — the connection pool every sql layer the endpoint registers stores its data<br/>
   /// through. Each sql layer contributes its own schema, created before the database's first use. The type-id interner the<br/>
   /// sql layers share is wired separately (on sqlite it lives in its own database, whose name is supplied to<br/>
   /// <c>SqliteTypeIdInterner(...)</c>; the domain database pairings derive it from the declaration instead).</summary>
   ///<remarks>A transaction takes the database's connection as its one durable resource, whose commit is the transaction's decision: the<br/>
   /// database commits once every other participant of the transaction has prepared, and before any is told the outcome. So a<br/>
   /// transaction can't also hold another durable resource, such as another database's connection or a resource manager of your own.<br/>
   /// When that takes part first, the write to this database throws, saying so. When it takes part second, .NET refuses it itself:<br/>
   /// "Implicit distributed transactions have not been enabled".<br/>
   /// With distributed transactions enabled, the connection refuses to prepare a two-phase commit, and the transaction rolls back.<br/>
   /// Keep each database, and each other durable resource, in transactions of its own.</remarks>
   public static IComponentRegistrar SqliteDomainDatabase(this IComponentRegistrar registrar, string connectionStringName) =>
      registrar.SqliteConnectionPool(connectionStringName);

   ///<summary>Declares the domain database this endpoint joins: the sqlite database in <paramref name="databaseFile"/>, which<br/>
   /// sqlite creates at the database's first use if it doesn't exist, in a folder that must. Otherwise as<br/>
   /// <see cref="SqliteDomainDatabase(IComponentRegistrar, string)"/>, with no configuration to give a name a connection string.</summary>
   ///<remarks>The file is used as it is in a testing container too, rather than a database of the test database pool: a<br/>
   /// consumer's specification that gives each scenario a file of its own needs no pool.</remarks>
   ///<remarks>A transaction takes the database's connection as its one durable resource, as the remarks of<br/>
   /// <see cref="SqliteDomainDatabase(IComponentRegistrar, string)"/> say: keep each database, and each other durable resource, in<br/>
   /// transactions of its own.</remarks>
   public static IComponentRegistrar SqliteDomainDatabase(this IComponentRegistrar registrar, FileInfo databaseFile) =>
      registrar.SqliteConnectionPool(databaseFile);
}
