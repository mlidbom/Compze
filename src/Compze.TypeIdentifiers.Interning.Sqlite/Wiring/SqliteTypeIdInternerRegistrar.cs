using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.Sqlite;
using Compze.Internals.Sql.Sqlite.Private;
using Compze.Internals.Sql.Sqlite.Wiring;

namespace Compze.TypeIdentifiers.Interning.Sqlite.Wiring;

public static class SqliteTypeIdInternerRegistrar
{
   ///<summary>Wires the SQLite type-id interner for the endpoint's declared database (<paramref name="endpointDatabase"/>): the<br/>
   /// interner's own database is named "«endpoint-database-name».TypeIdInterner" — this is where that naming convention lives.<br/>
   /// The sqlite feature pairings call this with the foundation's declaration; use the name-taking overload directly to share<br/>
   /// one interner database between endpoints.</summary>
   public static IComponentRegistrar SqliteTypeIdInterner(this IComponentRegistrar registrar, SqliteEndpointDatabase endpointDatabase) =>
      registrar.SqliteTypeIdInterner($"{endpointDatabase.ConnectionStringName}.TypeIdInterner");

   /// <summary>
   /// Wires the SQLite type-id interner against its own database, reached through
   /// <paramref name="internerConnectionStringName"/>. On SQLite the interner always uses a database separate from
   /// the business data (see <see cref="ISqliteTypeIdInternerConnectionPool"/>); point two domains at the same name
   /// to share one interner database, or at different names to keep them apart.
   /// </summary>
   public static IComponentRegistrar SqliteTypeIdInterner(this IComponentRegistrar registrar, string internerConnectionStringName)
   {
      if(registrar.IsRegistered<ITypeIdInterner>())
         return registrar;

      registrar.SqliteTypeIdInternerConnectionPool(internerConnectionStringName);

      return registrar.Register(
         Singleton.For<ITypeIdInternerPersistence>()
                  .CreatedBy((ISqliteTypeIdInternerConnectionPool connectionPool) =>
                                new SqliteTypeIdInternerPersistence(connectionPool, new SqliteSqlLayerSchemaManager(connectionPool, [SqliteTypeIdInternerPersistence.SchemaCreationSql]))),
         Singleton.For<ITypeIdInterner>()
                  .CreatedBy((ITypeIdInternerPersistence persistence, ITypeMap typeMap) => TypeIdInterner.For(persistence, typeMap)));
   }
}
