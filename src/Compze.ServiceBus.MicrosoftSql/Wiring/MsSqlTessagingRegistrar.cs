using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.MicrosoftSql;
using Compze.Internals.Sql.MicrosoftSql.Private;
using Compze.ServiceBus.Transport.SqlLayer;
using Compze.TypeIdentifiers.Interning;

namespace Compze.ServiceBus.MicrosoftSql.Wiring;

public static class MsSqlTessagingRegistrar
{
   public static string SchemaCreationSql => $"{MsSqlInboxSqlLayer.SchemaCreationSql}{Environment.NewLine}{Environment.NewLine}{MsSqlOutboxSqlLayer.SchemaCreationSql}";

   public static IComponentRegistrar MsSqlTessagingSqlLayer(this IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<IServiceBusSqlLayer.IOutboxSqlLayer>()
                  .CreatedBy((IMsSqlConnectionPool endpointSqlConnection, MsSqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) => new MsSqlOutboxSqlLayer(endpointSqlConnection, schemaManager, typeIdInterner)),
         Singleton.For<IServiceBusSqlLayer.IInboxSqlLayer>()
                  .CreatedBy((IMsSqlConnectionPool endpointSqlConnection, MsSqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) => new MsSqlInboxSqlLayer(endpointSqlConnection, schemaManager, typeIdInterner)));
}
