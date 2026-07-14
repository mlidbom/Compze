using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.Sqlite.Wiring;

namespace Compze.TypeIdentifiers.Interning.Sqlite.Wiring;

public static class SqliteEndpointPersistenceRegistrar
{
   ///<summary>Declares the endpoint's persistence: the sqlite database reached through <paramref name="connectionStringName"/> —<br/>
   /// the connection pool every sql-layer feature the endpoint adds stores its data through — plus the type-id interner, in its own<br/>
   /// database named "<paramref name="connectionStringName"/>.TypeIdInterner" (see<br/>
   /// <see cref="SqliteTypeIdInternerRegistrar.SqliteTypeIdInterner"/> for why the interner's database is separate on sqlite).<br/>
   /// Each sql-layer feature contributes its own schema, created before the database's first use — this one declaration is all the<br/>
   /// persistence wiring an endpoint needs before adding its sql-layer features.</summary>
   public static IComponentRegistrar SqliteEndpointPersistence(this IComponentRegistrar registrar, string connectionStringName) =>
      registrar.SqliteEndpointPersistence(connectionStringName, internerConnectionStringName: $"{connectionStringName}.TypeIdInterner");

   ///<summary>As <see cref="SqliteEndpointPersistence(IComponentRegistrar, string)"/>, naming the interner's database explicitly —<br/>
   /// point two endpoints' declarations at the same <paramref name="internerConnectionStringName"/> to share one interner database.</summary>
   public static IComponentRegistrar SqliteEndpointPersistence(this IComponentRegistrar registrar, string connectionStringName, string internerConnectionStringName) =>
      registrar.SqliteConnectionPool(connectionStringName)
               .SqliteTypeIdInterner(internerConnectionStringName);
}
