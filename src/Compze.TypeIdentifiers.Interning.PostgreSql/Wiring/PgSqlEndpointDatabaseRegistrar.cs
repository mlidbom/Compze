using Compze.Abstractions.Hosting.Public;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.PostgreSql;
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

   extension(EndpointFoundation @this)
   {
      ///<summary>Declares that the endpoint's database is PostgreSQL — see <see cref="PgSqlEndpointDatabase(IComponentRegistrar, string)"/>,<br/>
      /// to which this delegates. Returns the foundation typed by the declaration (<see cref="EndpointFoundation{TEndpointDatabase}"/>),<br/>
      /// so the features added on it bind their PostgreSQL sql layers through the compiler.</summary>
      public EndpointFoundation<PgSqlEndpointDatabase> PgSqlEndpointDatabase(string connectionStringName)
      {
         @this.Builder.Registrar.PgSqlEndpointDatabase(connectionStringName);
         return new EndpointFoundation<PgSqlEndpointDatabase>(@this.Builder, new PgSqlEndpointDatabase(connectionStringName));
      }
   }
}
