using Compze.Core.Tessaging.Teventive.TeventStore.Internal.SqlLayer.Abstractions;
using Compze.Internals.Sql.PostgreSql.Private;
using Compze.Internals.Sql.PostgreSql.Private.TEventStore;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Sql.PostgreSql.Wiring;

static class PgSqlTeventStoreRegistrar
{
   public static IComponentRegistrar PgSqlTeventStoreSqlLayer(this IComponentRegistrar registrar) =>
      registrar.Register(
                   Singleton.For<PgSqlTeventStoreConnectionManager>()
                            .CreatedBy((IPgSqlConnectionPool sqlConnectionProvider) => new PgSqlTeventStoreConnectionManager(sqlConnectionProvider)),
                   Singleton.For<ITeventStoreSqlLayer>()
                            .CreatedBy((PgSqlTeventStoreConnectionManager connectionManager, PgSqlSqlLayerSchemaManager schemaManager) => new PgSqlTeventStoreSqlLayer(connectionManager, schemaManager)))
               .PgSqlSqlLayerSchemaManager();
}
