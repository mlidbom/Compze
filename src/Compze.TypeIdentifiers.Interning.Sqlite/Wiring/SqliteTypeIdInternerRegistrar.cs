using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.Sqlite;
using Compze.Internals.Sql.Sqlite.Private;
using Compze.Internals.Sql.Sqlite.Wiring;

namespace Compze.TypeIdentifiers.Interning.Sqlite.Wiring;

public static class SqliteTypeIdInternerRegistrar
{
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
