using Compze.Tessaging.Teventive.TeventStore.Internal.SqlLayer.Abstractions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.PostgreSql;
using Compze.Internals.Sql.PostgreSql.Private;
using Compze.Internals.Sql.PostgreSql.Wiring;
using Compze.TypeIdentifiers.Interning;
using Layer = Compze.Tessaging.Teventive.TeventStore.PostgreSql.PgSqlTeventStoreSqlLayer;

namespace Compze.Tessaging.Teventive.TeventStore.PostgreSql.Wiring;

public static class PgSqlTeventStoreRegistrar
{
   public static IComponentRegistrar PgSqlTeventStoreSqlLayer(this IComponentRegistrar registrar) =>
      registrar.PgSqlSchemaContribution(Layer.SchemaCreationSql)
               .Register(
         Singleton.For<PgSqlTeventStoreConnectionManager>()
                  .CreatedBy((IPgSqlConnectionPool sqlConnectionProvider) => new PgSqlTeventStoreConnectionManager(sqlConnectionProvider)),
         Singleton.For<ITeventStoreSqlLayer>()
                  .CreatedBy((PgSqlTeventStoreConnectionManager connectionManager, PgSqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) => new Layer(connectionManager, schemaManager, typeIdInterner)));
}
