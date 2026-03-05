using Compze.Core.Tessaging.Teventive.TeventStore.Internal.SqlLayer.Abstractions;
using Compze.Internals.Sql.MicrosoftSql.Private;
using Compze.Internals.Sql.MicrosoftSql.Private.TEventStore;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Sql.MicrosoftSql.Wiring;

static class MsSqlTeventStoreRegistrar
{
   public static IComponentRegistrar MsSqlTeventStoreSqlLayer(this IComponentRegistrar registrar) =>
      registrar.MsSqlSqlLayerSchemaManager()
               .Register(
                   Singleton.For<MsSqlTeventStoreConnectionManager>()
                            .CreatedBy((IMsSqlConnectionPool sqlConnectionProvider) => new MsSqlTeventStoreConnectionManager(sqlConnectionProvider)),
                   Singleton.For<ITeventStoreSqlLayer>()
                            .CreatedBy((MsSqlTeventStoreConnectionManager connectionManager, MsSqlSqlLayerSchemaManager schemaManager) => new MsSqlTeventStoreSqlLayer(connectionManager, schemaManager)));
}
