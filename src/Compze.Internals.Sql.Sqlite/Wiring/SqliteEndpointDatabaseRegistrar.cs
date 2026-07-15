using Compze.Abstractions.Hosting.Public;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Sql.Sqlite.Wiring;

public static class SqliteEndpointDatabaseRegistrar
{
   ///<summary>Declares the endpoint's database: the sqlite database reached through <paramref name="connectionStringName"/> —<br/>
   /// the connection pool every sql-layer feature the endpoint adds stores its data through. Each sql-layer feature contributes<br/>
   /// its own schema, created before the database's first use. The type-id interner the sql-layer features share is wired<br/>
   /// separately (on sqlite it lives in its own database, whose name a registrar-level composition supplies to<br/>
   /// <c>SqliteTypeIdInterner(...)</c>; a composed endpoint's feature pairings derive it from the declaration instead).</summary>
   public static IComponentRegistrar SqliteEndpointDatabase(this IComponentRegistrar registrar, string connectionStringName) =>
      registrar.SqliteConnectionPool(connectionStringName);

   extension(EndpointFoundation @this)
   {
      ///<summary>Declares that the endpoint's database is sqlite — see <see cref="SqliteEndpointDatabase(IComponentRegistrar, string)"/>,<br/>
      /// to which this delegates. Returns the foundation typed by the declaration (<see cref="EndpointFoundation{TEndpointDatabase}"/>),<br/>
      /// so the features added on it bind their sqlite sql layers through the compiler (e.g. <c>AddExactlyOnceTessaging(...)</c> on a<br/>
      /// sqlite foundation registers Tessaging's sqlite sql layers).</summary>
      public EndpointFoundation<SqliteEndpointDatabase> SqliteEndpointDatabase(string connectionStringName)
      {
         @this.Builder.Registrar.SqliteEndpointDatabase(connectionStringName);
         return new EndpointFoundation<SqliteEndpointDatabase>(@this.Builder, new SqliteEndpointDatabase(connectionStringName));
      }
   }
}
