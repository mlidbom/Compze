using Compze.Core.Tessaging.Internal.SqlLayer;
using Compze.Internals.Sql.Common.Abstractions;
using Compze.Internals.Sql.MySql.Private;
using Compze.Internals.Sql.MySql.Private.Tessaging;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Sql.MySql.Wiring;

static class MySqlTessagingRegistrar
{
   public static IComponentRegistrar MySqlTessagingSqlLayer(this IComponentRegistrar registrar) =>
      registrar.MySqlTypeIdInterner()
               .Register(
                   Singleton.For<IServiceBusSqlLayer.IOutboxSqlLayer>()
                            .CreatedBy((IMySqlConnectionPool endpointSqlConnection, MySqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) => new MySqlOutboxSqlLayer(endpointSqlConnection, schemaManager, typeIdInterner)),
                   Singleton.For<IServiceBusSqlLayer.IInboxSqlLayer>()
                            .CreatedBy((IMySqlConnectionPool endpointSqlConnection, MySqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) => new MySqlInboxSqlLayer(endpointSqlConnection, schemaManager, typeIdInterner)))
               .MySqlSqlLayerSchemaManager();
}
