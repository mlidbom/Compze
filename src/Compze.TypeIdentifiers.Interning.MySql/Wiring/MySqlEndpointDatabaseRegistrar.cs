using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.MySql.Wiring;

namespace Compze.TypeIdentifiers.Interning.MySql.Wiring;

public static class MySqlEndpointDatabaseRegistrar
{
   ///<summary>Declares the endpoint's database: the MySQL database reached through <paramref name="connectionStringName"/> —<br/>
   /// the connection pool every sql-layer feature the endpoint adds stores its data through — plus the type-id interner, which on<br/>
   /// MySQL lives in that same database. Each sql-layer feature contributes its own schema, created before the database's<br/>
   /// first use — this one declaration is all the persistence wiring an endpoint needs before adding its sql-layer features.</summary>
   public static IComponentRegistrar MySqlEndpointDatabase(this IComponentRegistrar registrar, string connectionStringName) =>
      registrar.MySqlConnectionPool(connectionStringName)
               .MySqlTypeIdInterner();
}
