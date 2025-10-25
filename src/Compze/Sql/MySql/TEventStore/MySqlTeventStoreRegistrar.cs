using Compze.Core.Tessaging.Teventive.TeventStore.Internal.SqlLayer.Abstractions;
using Compze.Sql.MySql.SystemExtensions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Sql.MySql.TEventStore;

public static class MySqlTeventStoreRegistrar
{
   public static IComponentRegistrar MySqlTeventStore(this IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<MySqlTeventStoreConnectionManager>()
                  .CreatedBy((IMySqlConnectionPool sqlConnectionProvider) => new MySqlTeventStoreConnectionManager(sqlConnectionProvider)),
         Singleton.For<ITeventStoreSqlLayer>()
                  .CreatedBy((MySqlTeventStoreConnectionManager connectionManager) => new MySqlTeventStoreSqlLayer(connectionManager)));
}
