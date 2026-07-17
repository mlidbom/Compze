using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Sql.PostgreSql.Wiring;

public static class PgSqlEndpointDatabaseRegistrar
{
   ///<summary>Declares the endpoint's database: the PostgreSQL database reached through <paramref name="connectionStringName"/> —<br/>
   /// the connection pool every sql layer the endpoint registers stores its data through. Each sql layer contributes<br/>
   /// its own schema, created before the database's first use, and demands the type-id interner it shares with the others itself —<br/>
   /// this one declaration is all the persistence wiring an endpoint needs before its sql layers register.</summary>
   public static IComponentRegistrar PgSqlEndpointDatabase(this IComponentRegistrar registrar, string connectionStringName) =>
      registrar.PgSqlConnectionPoolIfNotAlreadyRegistered(connectionStringName);
}
