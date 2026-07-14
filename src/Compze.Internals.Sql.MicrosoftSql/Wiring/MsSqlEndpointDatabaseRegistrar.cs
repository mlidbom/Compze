using Compze.Abstractions.Hosting.Public;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Sql.MicrosoftSql.Wiring;

public static class MsSqlEndpointDatabaseRegistrar
{
   ///<summary>Declares the endpoint's database: the SQL Server database reached through <paramref name="connectionStringName"/> —<br/>
   /// the connection pool every sql-layer feature the endpoint adds stores its data through. Each sql-layer feature contributes<br/>
   /// its own schema, created before the database's first use, and demands the type-id interner it shares with the others itself —<br/>
   /// this one declaration is all the persistence wiring an endpoint needs before adding its sql-layer features.</summary>
   public static IComponentRegistrar MsSqlEndpointDatabase(this IComponentRegistrar registrar, string connectionStringName) =>
      registrar.MsSqlConnectionPool(connectionStringName);

   extension(EndpointFoundation @this)
   {
      ///<summary>Declares that the endpoint's database is SQL Server — see <see cref="MsSqlEndpointDatabase(IComponentRegistrar, string)"/>,<br/>
      /// to which this delegates. Returns the foundation typed by the declaration (<see cref="EndpointFoundation{TEndpointDatabase}"/>),<br/>
      /// so the features added on it bind their SQL Server sql layers through the compiler.</summary>
      public EndpointFoundation<MsSqlEndpointDatabase> MsSqlEndpointDatabase(string connectionStringName)
      {
         @this.Builder.Registrar.MsSqlEndpointDatabase(connectionStringName);
         return new EndpointFoundation<MsSqlEndpointDatabase>(@this.Builder, new MsSqlEndpointDatabase(connectionStringName));
      }
   }
}
