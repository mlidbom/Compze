using Compze.Sql.Common.TeventStore.Abstractions;
using Compze.Sql.MySql.SystemExtensions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Teventive.TeventStore.MySql;

public static class MySqlTeventStoreRegistrar
{
   public static IComponentRegistrar MySqlTeventStore(this IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<MySqlTeventStoreConnectionManager>()
                  .CreatedBy((IMySqlConnectionPool sqlConnectionProvider) => new MySqlTeventStoreConnectionManager(sqlConnectionProvider)),
         Singleton.For<ITeventStoreSqlLayer>()
                  .CreatedBy((MySqlTeventStoreConnectionManager connectionManager) => new MySqlTeventStoreSqlLayer(connectionManager)));
}
