using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.Sqlite.Private;

namespace Compze.Internals.Sql.Sqlite.Wiring;

public static class SqliteSqlLayerSchemaManagerRegistrar
{
   public static IComponentRegistrar SqliteSqlLayerSchemaManager(this IComponentRegistrar registrar, IReadOnlyList<string> schemaCreationScripts)
   {
      if(registrar.IsRegistered<Private.SqliteSqlLayerSchemaManager>())
         return registrar;

      return registrar.Register(Singleton.For<Private.SqliteSqlLayerSchemaManager>()
                                         .CreatedBy((ISqliteConnectionPool connectionPool) => new Private.SqliteSqlLayerSchemaManager(connectionPool, schemaCreationScripts))
                                         .DelegateToParentServiceLocatorWhenCloning());
   }
}
