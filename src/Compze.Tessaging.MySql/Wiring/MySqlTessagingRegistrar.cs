using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.MySql;
using Compze.Internals.Sql.MySql.Private;
using Compze.ServiceBus.Transport.SqlLayer;
using Compze.TypeIdentifiers.Interning;

namespace Compze.Tessaging.MySql.Wiring;

public static class MySqlTessagingRegistrar
{
   public static string SchemaCreationSql => $"{MySqlInboxSqlLayer.SchemaCreationSql}{Environment.NewLine}{Environment.NewLine}{MySqlOutboxSqlLayer.SchemaCreationSql}";

   public static IComponentRegistrar MySqlTessagingSqlLayer(this IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<IServiceBusSqlLayer.IOutboxSqlLayer>()
                  .CreatedBy((IMySqlConnectionPool endpointSqlConnection, MySqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) => new MySqlOutboxSqlLayer(endpointSqlConnection, schemaManager, typeIdInterner)),
         Singleton.For<IServiceBusSqlLayer.IInboxSqlLayer>()
                  .CreatedBy((IMySqlConnectionPool endpointSqlConnection, MySqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) => new MySqlInboxSqlLayer(endpointSqlConnection, schemaManager, typeIdInterner)));
}
