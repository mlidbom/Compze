using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Sql.Sqlite.Wiring;

public static class SqliteEndpointDatabaseRegistrar
{
   ///<summary>Declares the endpoint's database: the sqlite database reached through <paramref name="connectionStringName"/> —<br/>
   /// the connection pool every sql layer the endpoint registers stores its data through. Each sql layer contributes<br/>
   /// its own schema, created before the database's first use. The type-id interner the sql layers share is wired<br/>
   /// separately (on sqlite it lives in its own database, whose name is supplied to <c>SqliteTypeIdInterner(...)</c>;<br/>
   /// the endpoint database pairings derive it from the declaration instead).</summary>
   public static IComponentRegistrar SqliteEndpointDatabase(this IComponentRegistrar registrar, string connectionStringName) =>
      registrar.SqliteConnectionPool(connectionStringName);
}
