using Compze.Sql.Common.TeventStore.Abstractions;
using Compze.Sql.PostgreSql;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Teventive.TeventStore.PostgreSql;

public static class PgSqlTeventStoreRegistrar
{
   public static IComponentRegistrar PgSqlTeventStore(this IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<PgSqlTeventStoreConnectionManager>()
                  .CreatedBy((IPgSqlConnectionPool sqlConnectionProvider) => new PgSqlTeventStoreConnectionManager(sqlConnectionProvider)),
         Singleton.For<ITeventStoreSqlLayer>()
                  .CreatedBy((PgSqlTeventStoreConnectionManager connectionManager) => new PgSqlTeventStoreSqlLayer(connectionManager)));
}
