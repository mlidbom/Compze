using Compze.Internals.Sql.Common;
using Compze.Internals.Sql.Common.Abstractions;
using Compze.Internals.Sql.MySql.Private;
using Compze.Internals.Sql.MySql.Private.TypeIdInterning;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.TypeIdentifiers;

namespace Compze.Internals.Sql.MySql.Wiring;

static class MySqlTypeIdInternerRegistrar
{
   public static IComponentRegistrar MySqlTypeIdInterner(this IComponentRegistrar registrar)
   {
      if(registrar.IsRegistered<ITypeIdInterner>())
         return registrar;

      return registrar.Register(
         Singleton.For<ITypeIdInternerPersistence>()
                  .CreatedBy((IMySqlConnectionPool connectionPool, MySqlSqlLayerSchemaManager schemaManager) => new MySqlTypeIdInternerPersistence(connectionPool, schemaManager)),
         Singleton.For<ITypeIdInterner>()
                  .CreatedBy((ITypeIdInternerPersistence persistence, ITypeMap typeMap) => TypeIdInterner.For(persistence, typeMap)))
               .MySqlSqlLayerSchemaManager();
   }
}
