using Compze.DependencyInjection.Abstractions;

namespace Compze.Sql.Sqlite.Wiring;

public static class SqliteDomainDatabaseRegistrar
{
   ///<summary>Declares the domain database this endpoint joins: the sqlite database reached through<br/>
   /// <paramref name="connectionStringName"/> — the connection pool every sql layer the endpoint registers stores its data<br/>
   /// through. Each sql layer contributes its own schema, created before the database's first use. The type-id interner the<br/>
   /// sql layers share is wired separately (on sqlite it lives in its own database, whose name is supplied to<br/>
   /// <c>SqliteTypeIdInterner(...)</c>; the domain database pairings derive it from the declaration instead).</summary>
   public static IComponentRegistrar SqliteDomainDatabase(this IComponentRegistrar registrar, string connectionStringName) =>
      registrar.SqliteConnectionPool(connectionStringName);
}
