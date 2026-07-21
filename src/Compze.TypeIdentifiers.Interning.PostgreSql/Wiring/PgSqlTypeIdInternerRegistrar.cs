using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Sql.PostgreSql;
using Compze.Sql.PostgreSql.Wiring;
using Compze.TypeIdentifiers.Interning.PostgreSql._private;
using Compze.Sql.PostgreSql.Wiring._internal;
using Compze.Sql.PostgreSql._internal;

namespace Compze.TypeIdentifiers.Interning.PostgreSql.Wiring;

public static class PgSqlTypeIdInternerRegistrar
{
   public static IComponentRegistrar PgSqlTypeIdInterner(this IComponentRegistrar registrar)
   {
      if(registrar.IsRegistered<ITypeIdInterner>())
         return registrar;

      return registrar.PgSqlSchemaContribution(PgSqlTypeIdInternerPersistence.SchemaCreationSql)
                      .Register(
         Singleton.For<ITypeIdInternerPersistence>()
                  .CreatedBy((IPgSqlConnectionPool connectionPool, PgSqlSqlLayerSchemaManager schemaManager) => new PgSqlTypeIdInternerPersistence(connectionPool, schemaManager)),
         Singleton.For<ITypeIdInterner>()
                  .CreatedBy((ITypeIdInternerPersistence persistence, ITypeMap typeMap) => TypeIdInterner.For(persistence, typeMap)));
   }
}
