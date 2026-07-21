using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.MicrosoftSql;
using Compze.Internals.Sql.MicrosoftSql.Private;
using Compze.Internals.Sql.MicrosoftSql.Wiring;
using Compze.Teventive.TeventStore.Abstractions.Internal.SqlLayer.Abstractions;
using Compze.TypeIdentifiers.Interning;
using Compze.TypeIdentifiers.Interning.MicrosoftSql.Wiring;
using Layer = Compze.Teventive.TeventStore.MicrosoftSql.Private.MsSqlTeventStoreSqlLayer;
using Compze.Internals.Sql.MicrosoftSql.Wiring.Internal;
using Compze.Teventive.TeventStore.MicrosoftSql.Private;

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
