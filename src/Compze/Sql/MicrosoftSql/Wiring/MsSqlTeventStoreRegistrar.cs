using Compze.Core.Tessaging.Teventive.TeventStore.Internal.SqlLayer.Abstractions;
using Compze.Sql.MicrosoftSql.Private.TEventStore;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Sql.MicrosoftSql.Wiring;

public static class MsSqlTeventStoreRegistrar
{
   public static IComponentRegistrar MsSqlTeventStoreSqlLayer(this IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<MsSqlTeventStoreConnectionManager>()
                  .CreatedBy((IMsSqlConnectionPool sqlConnectionProvider) => new MsSqlTeventStoreConnectionManager(sqlConnectionProvider)),
         Singleton.For<ITeventStoreSqlLayer>()
                  .CreatedBy((MsSqlTeventStoreConnectionManager connectionManager) => new MsSqlTeventStoreSqlLayer(connectionManager)));
}
