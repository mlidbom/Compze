using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.PostgreSql.Wiring;

namespace Compze.TypeIdentifiers.Interning.PostgreSql.Wiring;

public static class PgSqlEndpointDatabaseRegistrar
{
   ///<summary>Declares the endpoint's database: the PostgreSQL database reached through <paramref name="connectionStringName"/> —<br/>
   /// the connection pool every sql-layer feature the endpoint adds stores its data through — plus the type-id interner, which on<br/>
   /// PostgreSQL lives in that same database. Each sql-layer feature contributes its own schema, created before the database's<br/>
   /// first use — this one declaration is all the persistence wiring an endpoint needs before adding its sql-layer features.</summary>
   public static IComponentRegistrar PgSqlEndpointDatabase(this IComponentRegistrar registrar, string connectionStringName) =>
      registrar.PgSqlConnectionPoolIfNotAlreadyRegistered(connectionStringName)
               .PgSqlTypeIdInterner();
}
