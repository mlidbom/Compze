using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.PostgreSql;
using Compze.Internals.Sql.PostgreSql.Private;
using Compze.Internals.Sql.PostgreSql.Wiring;
using Compze.Teventive.TeventStore.Abstractions.Internal.SqlLayer.Abstractions;
using Compze.TypeIdentifiers.Interning;
using Compze.TypeIdentifiers.Interning.PostgreSql.Wiring;
using Layer = Compze.Teventive.TeventStore.PostgreSql.Private.PgSqlTeventStoreSqlLayer;
using Compze.Internals.Sql.PostgreSql.Wiring.Internal;
using Compze.Teventive.TeventStore.PostgreSql.Private;

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
