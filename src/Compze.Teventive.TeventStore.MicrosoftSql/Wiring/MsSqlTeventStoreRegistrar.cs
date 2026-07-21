using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Sql.MicrosoftSql._internal;
using Compze.Teventive.TeventStore.Abstractions._internal.SqlLayer.Abstractions;
using Compze.TypeIdentifiers.Interning;
using Compze.TypeIdentifiers.Interning.MicrosoftSql.Wiring;
using Layer = Compze.Teventive.TeventStore.MicrosoftSql._private.MsSqlTeventStoreSqlLayer;
using Compze.Sql.MicrosoftSql.Wiring._internal;
using Compze.Teventive.TeventStore.MicrosoftSql._private;

namespace Compze.Teventive.TeventStore.MicrosoftSql.Wiring;

public static class MsSqlTeventStoreRegistrar
{
   public static IComponentRegistrar MsSqlTeventStoreSqlLayer(this IComponentRegistrar registrar) =>
      registrar.MsSqlTypeIdInterner()
               .MsSqlSchemaContribution(Layer.SchemaCreationSql)
               .Register(
         Singleton.For<MsSqlTeventStoreConnectionManager>()
                  .CreatedBy((IMsSqlConnectionPool sqlConnectionProvider) => new MsSqlTeventStoreConnectionManager(sqlConnectionProvider)),
         Singleton.For<ITeventStoreSqlLayer>()
                  .CreatedBy((MsSqlTeventStoreConnectionManager connectionManager, MsSqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) => new Layer(connectionManager, schemaManager, typeIdInterner)));
}
