using Compze.Abstractions.Hosting.Public;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.Sqlite;
using Compze.Internals.Sql.Sqlite.Wiring;

namespace Compze.TypeIdentifiers.Interning.Sqlite.Wiring;

public static class SqliteEndpointDatabaseRegistrar
{
   ///<summary>Declares the endpoint's database: the sqlite database reached through <paramref name="connectionStringName"/> —<br/>
   /// the connection pool every sql-layer feature the endpoint adds stores its data through — plus the type-id interner, in its own<br/>
   /// database named "<paramref name="connectionStringName"/>.TypeIdInterner" (see<br/>
   /// <see cref="SqliteTypeIdInternerRegistrar.SqliteTypeIdInterner"/> for why the interner's database is separate on sqlite).<br/>
   /// Each sql-layer feature contributes its own schema, created before the database's first use — this one declaration is all the<br/>
   /// persistence wiring an endpoint needs before adding its sql-layer features.</summary>
   public static IComponentRegistrar SqliteEndpointDatabase(this IComponentRegistrar registrar, string connectionStringName) =>
      registrar.SqliteEndpointDatabase(connectionStringName, internerConnectionStringName: $"{connectionStringName}.TypeIdInterner");

   ///<summary>As <see cref="SqliteEndpointDatabase(IComponentRegistrar, string)"/>, naming the interner's database explicitly —<br/>
   /// point two endpoints' declarations at the same <paramref name="internerConnectionStringName"/> to share one interner database.</summary>
   public static IComponentRegistrar SqliteEndpointDatabase(this IComponentRegistrar registrar, string connectionStringName, string internerConnectionStringName) =>
      registrar.SqliteConnectionPool(connectionStringName)
               .SqliteTypeIdInterner(internerConnectionStringName);

   extension(EndpointFoundation @this)
   {
      ///<summary>Declares that the endpoint's database is sqlite — see <see cref="SqliteEndpointDatabase(IComponentRegistrar, string)"/>,<br/>
      /// to which this delegates. Returns the foundation typed by the declaration (<see cref="EndpointFoundation{TEndpointDatabase}"/>),<br/>
      /// so the features added on it bind their sqlite sql layers through the compiler (e.g. <c>AddDistributedTessaging(...)</c> on a<br/>
      /// sqlite foundation registers Tessaging's sqlite sql layers).</summary>
      public EndpointFoundation<SqliteEndpointDatabase> SqliteEndpointDatabase(string connectionStringName)
      {
         @this.Builder.Registrar.SqliteEndpointDatabase(connectionStringName);
         return new EndpointFoundation<SqliteEndpointDatabase>(@this.Builder, new SqliteEndpointDatabase(connectionStringName));
      }
   }
}
