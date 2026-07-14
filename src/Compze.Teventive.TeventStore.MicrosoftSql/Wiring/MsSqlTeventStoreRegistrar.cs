using Compze.Tessaging.Teventive.TeventStore.Internal.SqlLayer.Abstractions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.MicrosoftSql;
using Compze.Internals.Sql.MicrosoftSql.Private;
using Compze.Internals.Sql.MicrosoftSql.Wiring;
using Compze.TypeIdentifiers.Interning;
using Layer = Compze.Tessaging.Teventive.TeventStore.MicrosoftSql.MsSqlTeventStoreSqlLayer;

namespace Compze.Tessaging.Teventive.TeventStore.MicrosoftSql.Wiring;

public static class MsSqlTeventStoreRegistrar
{
   public static IComponentRegistrar MsSqlTeventStoreSqlLayer(this IComponentRegistrar registrar) =>
      registrar.MsSqlSchemaContribution(Layer.SchemaCreationSql)
               .Register(
         Singleton.For<MsSqlTeventStoreConnectionManager>()
                  .CreatedBy((IMsSqlConnectionPool sqlConnectionProvider) => new MsSqlTeventStoreConnectionManager(sqlConnectionProvider)),
         Singleton.For<ITeventStoreSqlLayer>()
                  .CreatedBy((MsSqlTeventStoreConnectionManager connectionManager, MsSqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) => new Layer(connectionManager, schemaManager, typeIdInterner)));
}
