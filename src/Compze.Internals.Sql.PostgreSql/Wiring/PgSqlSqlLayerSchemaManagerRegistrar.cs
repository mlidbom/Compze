using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.PostgreSql.Private;

namespace Compze.Internals.Sql.PostgreSql.Wiring;

public static class PgSqlSqlLayerSchemaManagerRegistrar
{
   public static IComponentRegistrar PgSqlSqlLayerSchemaManager(this IComponentRegistrar registrar, IReadOnlyList<string> schemaCreationScripts)
   {
      if(registrar.IsRegistered<Private.PgSqlSqlLayerSchemaManager>())
         return registrar;

      return registrar.Register(Singleton.For<Private.PgSqlSqlLayerSchemaManager>()
                                         .CreatedBy((IPgSqlConnectionPool connectionPool) => new Private.PgSqlSqlLayerSchemaManager(connectionPool, schemaCreationScripts))
                                         .DelegateToParentServiceLocatorWhenCloning());
   }
}
