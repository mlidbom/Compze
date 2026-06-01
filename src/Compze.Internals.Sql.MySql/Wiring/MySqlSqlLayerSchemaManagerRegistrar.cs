using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.MySql.Private;

namespace Compze.Internals.Sql.MySql.Wiring;

public static class MySqlSqlLayerSchemaManagerRegistrar
{
   public static IComponentRegistrar MySqlSqlLayerSchemaManager(this IComponentRegistrar registrar, IReadOnlyList<string> schemaCreationScripts)
   {
      if(registrar.IsRegistered<Private.MySqlSqlLayerSchemaManager>())
         return registrar;

      return registrar.Register(Singleton.For<Private.MySqlSqlLayerSchemaManager>()
                                         .CreatedBy((IMySqlConnectionPool connectionPool) => new Private.MySqlSqlLayerSchemaManager(connectionPool, schemaCreationScripts))
                                         .DelegateToParentServiceLocatorWhenCloning());
   }
}
