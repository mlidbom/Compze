using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.Sqlite;
using Compze.Internals.Sql.Sqlite.Private;
using Compze.Internals.Sql.Sqlite.Wiring;
using Compze.Tessaging.Transport.SqlLayer;
using Compze.TypeIdentifiers.Interning;

namespace Compze.Tessaging.Sqlite.Wiring;

public static class SqliteTessagingRegistrar
{
   public static IComponentRegistrar SqliteTessagingSqlLayer(this IComponentRegistrar registrar) =>
      registrar.SqliteSchemaContribution(SqliteInboxSqlLayer.SchemaCreationSql)
               .SqliteSchemaContribution(SqliteOutboxSqlLayer.SchemaCreationSql)
               .Register(
         Singleton.For<IServiceBusSqlLayer.IOutboxSqlLayer>()
                  .CreatedBy((ISqliteConnectionPool endpointSqlConnection, SqliteSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) => new SqliteOutboxSqlLayer(endpointSqlConnection, schemaManager, typeIdInterner)),
         Singleton.For<IServiceBusSqlLayer.IInboxSqlLayer>()
                  .CreatedBy((ISqliteConnectionPool endpointSqlConnection, SqliteSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) => new SqliteInboxSqlLayer(endpointSqlConnection, schemaManager, typeIdInterner)));
}
