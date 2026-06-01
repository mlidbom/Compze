using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.MySql;
using Compze.Internals.Sql.MySql.Private;

namespace Compze.TypeIdentifiers.Interning.MySql.Wiring;

public static class MySqlTypeIdInternerRegistrar
{
   public static string SchemaCreationSql => MySqlTypeIdInternerPersistence.SchemaCreationSql;

   public static IComponentRegistrar MySqlTypeIdInterner(this IComponentRegistrar registrar)
   {
      if(registrar.IsRegistered<ITypeIdInterner>())
         return registrar;

      return registrar.Register(
         Singleton.For<ITypeIdInternerPersistence>()
                  .CreatedBy((IMySqlConnectionPool connectionPool, MySqlSqlLayerSchemaManager schemaManager) => new MySqlTypeIdInternerPersistence(connectionPool, schemaManager)),
         Singleton.For<ITypeIdInterner>()
                  .CreatedBy((ITypeIdInternerPersistence persistence, ITypeMap typeMap) => TypeIdInterner.For(persistence, typeMap)));
   }
}
