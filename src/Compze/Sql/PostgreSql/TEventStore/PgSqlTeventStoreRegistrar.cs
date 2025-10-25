using Compze.Core.Tessaging.Teventive.TeventStore.Internal.SqlLayer.Abstractions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Sql.PostgreSql.TEventStore;

public static class PgSqlTeventStoreRegistrar
{
   public static IComponentRegistrar PgSqlTeventStore(this IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<PgSqlTeventStoreConnectionManager>()
                  .CreatedBy((IPgSqlConnectionPool sqlConnectionProvider) => new PgSqlTeventStoreConnectionManager(sqlConnectionProvider)),
         Singleton.For<ITeventStoreSqlLayer>()
                  .CreatedBy((PgSqlTeventStoreConnectionManager connectionManager) => new PgSqlTeventStoreSqlLayer(connectionManager)));
}
