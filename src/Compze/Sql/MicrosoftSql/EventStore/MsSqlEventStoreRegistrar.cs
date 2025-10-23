using Compze.Sql.Common.EventStore.Abstractions;
using Compze.Sql.MicrosoftSql;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Teventive.EventStore.MicrosoftSql;

public static class MsSqlEventStoreRegistrar
{
   public static IComponentRegistrar MsSqlEventStore(this IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<MsSqlEventStoreConnectionManager>()
                  .CreatedBy((IMsSqlConnectionPool sqlConnectionProvider) => new MsSqlEventStoreConnectionManager(sqlConnectionProvider)),
         Singleton.For<IEventStoreSqlLayer>()
                  .CreatedBy((MsSqlEventStoreConnectionManager connectionManager) => new MsSqlEventStoreSqlLayer(connectionManager)));
}
