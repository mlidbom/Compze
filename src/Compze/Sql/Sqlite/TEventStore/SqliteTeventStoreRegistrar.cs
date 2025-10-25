using Compze.Core.Tessaging.Teventive.TeventStore.Internal.SqlLayer.Abstractions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Sql.Sqlite.TEventStore;

public static class SqliteTeventStoreRegistrar
{
   public static IComponentRegistrar SqliteTeventStore(this IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<SqliteTeventStoreConnectionManager>()
                  .CreatedBy((ISqliteConnectionPool sqlConnectionProvider) => new SqliteTeventStoreConnectionManager(sqlConnectionProvider)),
         Singleton.For<ITeventStoreSqlLayer>()
                  .CreatedBy((SqliteTeventStoreConnectionManager connectionManager) => new SqliteTeventStoreSqlLayer(connectionManager)));
}
