using Compze.Internals.Sql.Common;
using Compze.Internals.Sql.Common.Abstractions;
using Compze.Internals.Sql.Sqlite.Private;
using Compze.Internals.Sql.Sqlite.Private.TypeIdInterning;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.TypeIdentifiers;

namespace Compze.Internals.Sql.Sqlite.Wiring;

static class SqliteTypeIdInternerRegistrar
{
   public static IComponentRegistrar SqliteTypeIdInterner(this IComponentRegistrar registrar)
   {
      if(registrar.IsRegistered<ITypeIdInterner>())
         return registrar;

      return registrar.Register(
         Singleton.For<ITypeIdInternerPersistence>()
                  .CreatedBy((ISqliteConnectionPool connectionPool, SqliteSqlLayerSchemaManager schemaManager) => new SqliteTypeIdInternerPersistence(connectionPool, schemaManager)),
         Singleton.For<ITypeIdInterner>()
                  .CreatedBy((ITypeIdInternerPersistence persistence, ITypeMap typeMap) => TypeIdInterner.For(persistence, typeMap)))
               .SqliteSqlLayerSchemaManager();
   }
}
