using Compze.Core.DocumentDb.Internal.SqlLayer;
using Compze.Internals.Sql.PostgreSql.Private;
using Compze.Internals.Sql.PostgreSql.Private.DocumentDb;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Sql.PostgreSql.Wiring;

static class PgSqlDocumentDbRegistrar
{
   public static IComponentRegistrar PgSqlDocumentDbSqlLayer(this IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<IDocumentDbSqlLayer>()
                  .CreatedBy((IPgSqlConnectionPool connectionProvider, PgSqlSqlLayerSchemaManager schemaManager) => new PgSqlDocumentDbSqlLayer(connectionProvider, schemaManager)))
               .PgSqlSqlLayerSchemaManager();
}
