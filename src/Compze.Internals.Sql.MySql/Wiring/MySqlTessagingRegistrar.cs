using Compze.Core.Tessaging.Internal.SqlLayer;
using Compze.Internals.Sql.MySql.Private;
using Compze.Internals.Sql.MySql.Private.Tessaging;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Sql.MySql.Wiring;

static class MySqlTessagingRegistrar
{
   public static IComponentRegistrar MySqlTessagingSqlLayer(this IComponentRegistrar registrar) =>
      registrar.Register(
                   Singleton.For<IServiceBusSqlLayer.IOutboxSqlLayer>()
                            .CreatedBy((IMySqlConnectionPool endpointSqlConnection, MySqlSqlLayerSchemaManager schemaManager) => new MySqlOutboxSqlLayer(endpointSqlConnection, schemaManager)),
                   Singleton.For<IServiceBusSqlLayer.IInboxSqlLayer>()
                            .CreatedBy((IMySqlConnectionPool endpointSqlConnection, MySqlSqlLayerSchemaManager schemaManager) => new MySqlInboxSqlLayer(endpointSqlConnection, schemaManager)))
               .MySqlSqlLayerSchemaManager();
}
