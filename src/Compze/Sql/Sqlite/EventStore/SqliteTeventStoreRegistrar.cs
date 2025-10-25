using Compze.Sql.Common.TeventStore.Abstractions;
using Compze.Sql.Sqlite;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Teventive.TeventStore.Sqlite;

public static class SqliteTeventStoreRegistrar
{
   public static IComponentRegistrar SqliteTeventStore(this IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<SqliteTeventStoreConnectionManager>()
                  .CreatedBy((ISqliteConnectionPool sqlConnectionProvider) => new SqliteTeventStoreConnectionManager(sqlConnectionProvider)),
         Singleton.For<ITeventStoreSqlLayer>()
                  .CreatedBy((SqliteTeventStoreConnectionManager connectionManager) => new SqliteTeventStoreSqlLayer(connectionManager)));
}
