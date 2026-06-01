using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.PostgreSql;
using Compze.Internals.Sql.PostgreSql.Private;

namespace Compze.TypeIdentifiers.Interning.PostgreSql.Wiring;

public static class PgSqlTypeIdInternerRegistrar
{
   public static string SchemaCreationSql => PgSqlTypeIdInternerPersistence.SchemaCreationSql;

   public static IComponentRegistrar PgSqlTypeIdInterner(this IComponentRegistrar registrar)
   {
      if(registrar.IsRegistered<ITypeIdInterner>())
         return registrar;

      return registrar.Register(
         Singleton.For<ITypeIdInternerPersistence>()
                  .CreatedBy((IPgSqlConnectionPool connectionPool, PgSqlSqlLayerSchemaManager schemaManager) => new PgSqlTypeIdInternerPersistence(connectionPool, schemaManager)),
         Singleton.For<ITypeIdInterner>()
                  .CreatedBy((ITypeIdInternerPersistence persistence, ITypeMap typeMap) => TypeIdInterner.For(persistence, typeMap)));
   }
}
