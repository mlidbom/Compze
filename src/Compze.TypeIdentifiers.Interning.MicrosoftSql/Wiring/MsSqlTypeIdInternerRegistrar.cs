using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Sql.MicrosoftSql;
using Compze.Sql.MicrosoftSql.Wiring;
using Compze.TypeIdentifiers.Interning.MicrosoftSql._private;
using Compze.Sql.MicrosoftSql.Wiring._internal;
using Compze.Sql.MicrosoftSql._internal;

namespace Compze.TypeIdentifiers.Interning.MicrosoftSql.Wiring;

public static class MsSqlTypeIdInternerRegistrar
{
   public static IComponentRegistrar MsSqlTypeIdInterner(this IComponentRegistrar registrar)
   {
      if(registrar.IsRegistered<ITypeIdInterner>())
         return registrar;

      return registrar.MsSqlSchemaContribution(MsSqlTypeIdInternerPersistence.SchemaCreationSql)
                      .Register(
         Singleton.For<ITypeIdInternerPersistence>()
                  .CreatedBy((IMsSqlConnectionPool connectionPool, MsSqlSqlLayerSchemaManager schemaManager) => new MsSqlTypeIdInternerPersistence(connectionPool, schemaManager)),
         Singleton.For<ITypeIdInterner>()
                  .CreatedBy((ITypeIdInternerPersistence persistence, ITypeMap typeMap) => TypeIdInterner.For(persistence, typeMap)));
   }
}
