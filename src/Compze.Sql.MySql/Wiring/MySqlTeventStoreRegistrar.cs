using Compze.Core.Tessaging.Teventive.TeventStore.Internal.SqlLayer.Abstractions;
using Compze.Sql.MySql.Private;
using Compze.Sql.MySql.Private.TEventStore;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Sql.MySql.Wiring;

internal static class MySqlTeventStoreRegistrar
{
   public static IComponentRegistrar MySqlTeventStoreSqlLayer(this IComponentRegistrar registrar) =>
      registrar.Register(
                   Singleton.For<MySqlTeventStoreConnectionManager>()
                            .CreatedBy((IMySqlConnectionPool sqlConnectionProvider) => new MySqlTeventStoreConnectionManager(sqlConnectionProvider)),
                   Singleton.For<ITeventStoreSqlLayer>()
                            .CreatedBy((MySqlTeventStoreConnectionManager connectionManager, MySqlSqlLayerSchemaManager schemaManager) => new MySqlTeventStoreSqlLayer(connectionManager, schemaManager)))
               .MySqlSqlLayerSchemaManager();
}
