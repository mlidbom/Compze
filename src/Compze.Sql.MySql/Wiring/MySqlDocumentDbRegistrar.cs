using Compze.Core.DocumentDb.Internal.SqlLayer;
using Compze.Sql.MySql.Private;
using Compze.Sql.MySql.Private.DocumentDb;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Sql.MySql.Wiring;

static class MySqlDocumentDbRegistrar
{
   public static IComponentRegistrar MySqlDocumentDbSqlLayer(this IComponentRegistrar registrar) =>
      registrar.Register(
                   Singleton.For<IDocumentDbSqlLayer>()
                            .CreatedBy((IMySqlConnectionPool connectionProvider, MySqlSqlLayerSchemaManager schemaManager) => new MySqlDocumentDbSqlLayer(connectionProvider, schemaManager)))
               .MySqlSqlLayerSchemaManager();
}
