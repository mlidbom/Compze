using Compze.Core.Tessaging.Teventive.TeventStore.Internal.SqlLayer.Abstractions;
using Compze.Internals.Sql.MySql.Private;
using Compze.Internals.Sql.MySql.Private.TEventStore;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Sql.MySql.Wiring;

static class MySqlTeventStoreRegistrar
{
   public static IComponentRegistrar MySqlTeventStoreSqlLayer(this IComponentRegistrar registrar) =>
      registrar.Register(
                   Singleton.For<MySqlTeventStoreConnectionManager>()
                            .CreatedBy((IMySqlConnectionPool sqlConnectionProvider) => new MySqlTeventStoreConnectionManager(sqlConnectionProvider)),
                   Singleton.For<ITeventStoreSqlLayer>()
                            .CreatedBy((MySqlTeventStoreConnectionManager connectionManager, MySqlSqlLayerSchemaManager schemaManager) => new MySqlTeventStoreSqlLayer(connectionManager, schemaManager)))
               .MySqlSqlLayerSchemaManager();
}
