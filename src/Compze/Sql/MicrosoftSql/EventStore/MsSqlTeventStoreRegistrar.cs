using Compze.Sql.Common.TeventStore.Abstractions;
using Compze.Sql.MicrosoftSql;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Teventive.TeventStore.MicrosoftSql;

public static class MsSqlTeventStoreRegistrar
{
   public static IComponentRegistrar MsSqlTeventStore(this IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<MsSqlTeventStoreConnectionManager>()
                  .CreatedBy((IMsSqlConnectionPool sqlConnectionProvider) => new MsSqlTeventStoreConnectionManager(sqlConnectionProvider)),
         Singleton.For<ITeventStoreSqlLayer>()
                  .CreatedBy((MsSqlTeventStoreConnectionManager connectionManager) => new MsSqlTeventStoreSqlLayer(connectionManager)));
}
