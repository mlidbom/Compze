using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Sql.MySql;
using Compze.Sql.MySql.Wiring;
using Compze.TypeIdentifiers.Interning.MySql._private;
using Compze.Sql.MySql.Wiring._internal;
using Compze.Sql.MySql._internal;

namespace Compze.TypeIdentifiers.Interning.MySql.Wiring;

public static class MySqlTypeIdInternerRegistrar
{
   public static IComponentRegistrar MySqlTypeIdInterner(this IComponentRegistrar registrar)
   {
      if(registrar.IsRegistered<ITypeIdInterner>())
         return registrar;

      return registrar.MySqlSchemaContribution(MySqlTypeIdInternerPersistence.SchemaCreationSql)
                      .Register(
         Singleton.For<ITypeIdInternerPersistence>()
                  .CreatedBy((IMySqlConnectionPool connectionPool, MySqlSqlLayerSchemaManager schemaManager) => new MySqlTypeIdInternerPersistence(connectionPool, schemaManager)),
         Singleton.For<ITypeIdInterner>()
                  .CreatedBy((ITypeIdInternerPersistence persistence, ITypeMap typeMap) => TypeIdInterner.For(persistence, typeMap)));
   }
}
