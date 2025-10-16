using Compze.Sql.MySql.Infrastructure.SystemExtensions;
using Compze.Tessaging.Teventive.EventStore.SqlLayer.Abstractions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Teventive.EventStore.MySql;

public static class MySqlEventStoreRegistrar
{
   public static IComponentRegistrar MySqlEventStore(this IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<MySqlEventStoreConnectionManager>()
                  .CreatedBy((IMySqlConnectionPool sqlConnectionProvider) => new MySqlEventStoreConnectionManager(sqlConnectionProvider)),
         Singleton.For<IEventStoreSqlLayer>()
                  .CreatedBy((MySqlEventStoreConnectionManager connectionManager) => new MySqlEventStoreSqlLayer(connectionManager)));
}
