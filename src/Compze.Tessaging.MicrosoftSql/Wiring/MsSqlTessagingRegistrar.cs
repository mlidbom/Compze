using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.MicrosoftSql;
using Compze.Internals.Sql.MicrosoftSql.Private;
using Compze.Internals.Sql.MicrosoftSql.Wiring;
using Compze.Tessaging.Transport.SqlLayer;
using Compze.TypeIdentifiers.Interning;

namespace Compze.Tessaging.MicrosoftSql.Wiring;

public static class MsSqlTessagingRegistrar
{
   public static IComponentRegistrar MsSqlTessagingSqlLayer(this IComponentRegistrar registrar) =>
      registrar.MsSqlSchemaContribution(MsSqlInboxSqlLayer.SchemaCreationSql)
               .MsSqlSchemaContribution(MsSqlOutboxSqlLayer.SchemaCreationSql)
               .Register(
         Singleton.For<IServiceBusSqlLayer.IOutboxSqlLayer>()
                  .CreatedBy((IMsSqlConnectionPool endpointSqlConnection, MsSqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) => new MsSqlOutboxSqlLayer(endpointSqlConnection, schemaManager, typeIdInterner)),
         Singleton.For<IServiceBusSqlLayer.IInboxSqlLayer>()
                  .CreatedBy((IMsSqlConnectionPool endpointSqlConnection, MsSqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) => new MsSqlInboxSqlLayer(endpointSqlConnection, schemaManager, typeIdInterner)));
}
