using Compze.Internals.Sql.Common;
using Compze.Internals.Sql.Common.Abstractions;
using Compze.Internals.Sql.MicrosoftSql.Private;
using Compze.Internals.Sql.MicrosoftSql.Private.TypeIdInterning;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.TypeIdentifiers;

namespace Compze.Internals.Sql.MicrosoftSql.Wiring;

static class MsSqlTypeIdInternerRegistrar
{
   public static IComponentRegistrar MsSqlTypeIdInterner(this IComponentRegistrar registrar)
   {
      if(registrar.IsRegistered<ITypeIdInterner>())
         return registrar;

      return registrar.Register(
         Singleton.For<ITypeIdInternerPersistence>()
                  .CreatedBy((IMsSqlConnectionPool connectionPool, MsSqlSqlLayerSchemaManager schemaManager) => new MsSqlTypeIdInternerPersistence(connectionPool, schemaManager)),
         Singleton.For<ITypeIdInterner>()
                  .CreatedBy((ITypeIdInternerPersistence persistence, ITypeMap typeMap) => TypeIdInterner.For(persistence, typeMap)))
               .MsSqlSqlLayerSchemaManager();
   }
}
