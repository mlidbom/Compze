using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.MySql;
using Compze.Internals.Sql.MySql.Wiring;
using Compze.Teventive.TeventStore.Abstractions._internal.SqlLayer.Abstractions;
using Compze.TypeIdentifiers.Interning;
using Compze.TypeIdentifiers.Interning.MySql.Wiring;
using Layer = Compze.Teventive.TeventStore.MySql._private.MySqlTeventStoreSqlLayer;
using Compze.Internals.Sql.MySql.Wiring._internal;
using Compze.Teventive.TeventStore.MySql._private;
using Compze.Internals.Sql.MySql._internal;

namespace Compze.Teventive.TeventStore.MySql.Wiring;

public static class MySqlTeventStoreRegistrar
{
   public static IComponentRegistrar MySqlTeventStoreSqlLayer(this IComponentRegistrar registrar) =>
      registrar.MySqlTypeIdInterner()
               .MySqlSchemaContribution(Layer.SchemaCreationSql)
               .Register(
         Singleton.For<MySqlTeventStoreConnectionManager>()
                  .CreatedBy((IMySqlConnectionPool sqlConnectionProvider) => new MySqlTeventStoreConnectionManager(sqlConnectionProvider)),
         Singleton.For<ITeventStoreSqlLayer>()
                  .CreatedBy((MySqlTeventStoreConnectionManager connectionManager, MySqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) => new Layer(connectionManager, schemaManager, typeIdInterner)));
}
