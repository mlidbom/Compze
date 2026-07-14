using Compze.Abstractions.Hosting.Public;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.MySql;
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

   extension(EndpointFoundation @this)
   {
      ///<summary>Declares that the endpoint's database is MySQL — see <see cref="MySqlEndpointDatabase(IComponentRegistrar, string)"/>,<br/>
      /// to which this delegates. Returns the foundation typed by the declaration (<see cref="EndpointFoundation{TEndpointDatabase}"/>),<br/>
      /// so the features added on it bind their MySQL sql layers through the compiler.</summary>
      public EndpointFoundation<MySqlEndpointDatabase> MySqlEndpointDatabase(string connectionStringName)
      {
         @this.Builder.Registrar.MySqlEndpointDatabase(connectionStringName);
         return new EndpointFoundation<MySqlEndpointDatabase>(@this.Builder, new MySqlEndpointDatabase(connectionStringName));
      }
   }
}
