using Compze.DocumentDb.Internal.SqlLayer;
using Compze.Internals.Sql.Common.Abstractions;
using Compze.Internals.Sql.MySql.Private;
using Compze.Internals.Sql.MySql.Private.DocumentDb;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Sql.MySql.Wiring;

static class MySqlDocumentDbRegistrar
{
   public static IComponentRegistrar MySqlDocumentDbSqlLayer(this IComponentRegistrar registrar) =>
      registrar.Register(
                   Singleton.For<IDocumentDbSqlLayer>()
                            .CreatedBy((IMySqlConnectionPool connectionProvider, MySqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) => new MySqlDocumentDbSqlLayer(connectionProvider, schemaManager, typeIdInterner)))
               .MySqlTypeIdInterner();
}
