using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.Sqlite;
using Compze.Internals.Sql.Sqlite.Private;

namespace Compze.TypeIdentifiers.Interning.Sqlite.Wiring;

public static class SqliteTypeIdInternerRegistrar
{
   public static string SchemaCreationSql => SqliteTypeIdInternerPersistence.SchemaCreationSql;

   public static IComponentRegistrar SqliteTypeIdInterner(this IComponentRegistrar registrar)
   {
      if(registrar.IsRegistered<ITypeIdInterner>())
         return registrar;

      return registrar.Register(
         Singleton.For<ITypeIdInternerPersistence>()
                  .CreatedBy((ISqliteConnectionPool connectionPool, SqliteSqlLayerSchemaManager schemaManager) => new SqliteTypeIdInternerPersistence(connectionPool, schemaManager)),
         Singleton.For<ITypeIdInterner>()
                  .CreatedBy((ITypeIdInternerPersistence persistence, ITypeMap typeMap) => TypeIdInterner.For(persistence, typeMap)));
   }
}
