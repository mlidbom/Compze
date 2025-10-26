using Compze.Core.DocumentDb.Internal.SqlLayer;
using Compze.Sql.PostgreSql.Private;
using Compze.Sql.PostgreSql.Private.DocumentDb;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Sql.PostgreSql.Wiring;

public static class PgSqlDocumentDbRegistrar
{
   public static IComponentRegistrar PgSqlDocumentDbSqlLayer(this IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<IDocumentDbSqlLayer>()
                  .CreatedBy((IPgSqlConnectionPool connectionProvider, PgSqlSqlLayerSchemaManager schemaManager) => new PgSqlDocumentDbSqlLayer(connectionProvider, schemaManager)))
               .PgSqlSqlLayerSchemaManager();
}
