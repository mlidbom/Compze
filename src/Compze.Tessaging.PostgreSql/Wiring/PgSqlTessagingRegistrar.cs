using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.PostgreSql;
using Compze.Internals.Sql.PostgreSql.Private;
using Compze.Tessaging.Transport.SqlLayer;
using Compze.TypeIdentifiers.Interning;

namespace Compze.Tessaging.PostgreSql.Wiring;

public static class PgSqlTessagingRegistrar
{
   public static string SchemaCreationSql => $"{PgSqlInboxSqlLayer.SchemaCreationSql}{Environment.NewLine}{Environment.NewLine}{PgSqlOutboxSqlLayer.SchemaCreationSql}";

   public static IComponentRegistrar PgSqlTessagingSqlLayer(this IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<IServiceBusSqlLayer.IOutboxSqlLayer>()
                  .CreatedBy((IPgSqlConnectionPool endpointSqlConnection, PgSqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) => new PgSqlOutboxSqlLayer(endpointSqlConnection, schemaManager, typeIdInterner)),
         Singleton.For<IServiceBusSqlLayer.IInboxSqlLayer>()
                  .CreatedBy((IPgSqlConnectionPool endpointSqlConnection, PgSqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) => new PgSqlInboxSqlLayer(endpointSqlConnection, schemaManager, typeIdInterner)));
}
