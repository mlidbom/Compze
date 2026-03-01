using Compze.Core.Tessaging.Teventive.TeventStore.Internal.SqlLayer.Abstractions;
using Compze.Sql.PostgreSql.Private;
using Compze.Sql.PostgreSql.Private.TEventStore;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Sql.PostgreSql.Wiring;

internal static class PgSqlTeventStoreRegistrar
{
   public static IComponentRegistrar PgSqlTeventStoreSqlLayer(this IComponentRegistrar registrar) =>
      registrar.Register(
                   Singleton.For<PgSqlTeventStoreConnectionManager>()
                            .CreatedBy((IPgSqlConnectionPool sqlConnectionProvider) => new PgSqlTeventStoreConnectionManager(sqlConnectionProvider)),
                   Singleton.For<ITeventStoreSqlLayer>()
                            .CreatedBy((PgSqlTeventStoreConnectionManager connectionManager, PgSqlSqlLayerSchemaManager schemaManager) => new PgSqlTeventStoreSqlLayer(connectionManager, schemaManager)))
               .PgSqlSqlLayerSchemaManager();
}
