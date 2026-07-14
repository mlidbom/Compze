using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.MicrosoftSql.Wiring;

namespace Compze.TypeIdentifiers.Interning.MicrosoftSql.Wiring;

public static class MsSqlEndpointPersistenceRegistrar
{
   ///<summary>Declares the endpoint's persistence: the SQL Server database reached through <paramref name="connectionStringName"/> —<br/>
   /// the connection pool every sql-layer feature the endpoint adds stores its data through — plus the type-id interner, which on<br/>
   /// SQL Server lives in that same database. Each sql-layer feature contributes its own schema, created before the database's<br/>
   /// first use — this one declaration is all the persistence wiring an endpoint needs before adding its sql-layer features.</summary>
   public static IComponentRegistrar MsSqlEndpointPersistence(this IComponentRegistrar registrar, string connectionStringName) =>
      registrar.MsSqlConnectionPool(connectionStringName)
               .MsSqlTypeIdInterner();
}
