using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Sql.PostgreSql;
using Compze.Sql.PostgreSql.Wiring;
using Compze.Teventive.TeventStore.Abstractions._internal.SqlLayer.Abstractions;
using Compze.TypeIdentifiers.Interning;
using Compze.TypeIdentifiers.Interning.PostgreSql.Wiring;
using Layer = Compze.Teventive.TeventStore.PostgreSql._private.PgSqlTeventStoreSqlLayer;
using Compze.Sql.PostgreSql.Wiring._internal;
using Compze.Teventive.TeventStore.PostgreSql._private;
using Compze.Sql.PostgreSql._internal;

namespace Compze.Teventive.TeventStore.PostgreSql.Wiring;

public static class PgSqlTeventStoreRegistrar
{
   public static IComponentRegistrar PgSqlTeventStoreSqlLayer(this IComponentRegistrar registrar) =>
      registrar.PgSqlTypeIdInterner()
               .PgSqlSchemaContribution(Layer.SchemaCreationSql)
               .Register(
         Singleton.For<PgSqlTeventStoreConnectionManager>()
                  .CreatedBy((IPgSqlConnectionPool sqlConnectionProvider) => new PgSqlTeventStoreConnectionManager(sqlConnectionProvider)),
         Singleton.For<ITeventStoreSqlLayer>()
                  .CreatedBy((PgSqlTeventStoreConnectionManager connectionManager, PgSqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) => new Layer(connectionManager, schemaManager, typeIdInterner)));
}
