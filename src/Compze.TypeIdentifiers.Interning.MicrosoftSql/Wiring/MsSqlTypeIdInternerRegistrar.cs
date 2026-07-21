using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.MicrosoftSql;
using Compze.Internals.Sql.MicrosoftSql.Wiring;
using Compze.TypeIdentifiers.Interning.MicrosoftSql.Private;
using Compze.Internals.Sql.MicrosoftSql.Wiring.Internal;
using Compze.Internals.Sql.MicrosoftSql.Internal;

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
