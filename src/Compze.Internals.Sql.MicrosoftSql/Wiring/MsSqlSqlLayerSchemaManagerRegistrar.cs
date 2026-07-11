using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.MicrosoftSql.Private;

namespace Compze.Internals.Sql.MicrosoftSql.Wiring;

public static class MsSqlSqlLayerSchemaManagerRegistrar
{
   public static IComponentRegistrar MsSqlSqlLayerSchemaManager(this IComponentRegistrar registrar, IReadOnlyList<string> schemaCreationScripts)
   {
      if(registrar.IsRegistered<Private.MsSqlSqlLayerSchemaManager>())
         return registrar;

      return registrar.Register(Singleton.For<Private.MsSqlSqlLayerSchemaManager>()
                                         .DelegateToParentServiceLocatorWhenCloning()
                                         .CreatedBy((IMsSqlConnectionPool connectionPool) => new Private.MsSqlSqlLayerSchemaManager(connectionPool, schemaCreationScripts)));
   }
}
