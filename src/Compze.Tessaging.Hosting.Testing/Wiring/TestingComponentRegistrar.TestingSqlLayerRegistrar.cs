using Compze.Abstractions.Wiring.Testing.Internal;
using Compze.Internals.Sql.MicrosoftSql.Wiring;
using Compze.Internals.Sql.MySql.Wiring;
using Compze.Internals.Sql.PostgreSql.Wiring;
using Compze.Internals.Sql.Sqlite.Wiring;
using Compze.DocumentDb.MicrosoftSql.Wiring;
using Compze.DocumentDb.MySql.Wiring;
using Compze.DocumentDb.PostgreSql.Wiring;
using Compze.DocumentDb.Sqlite.Wiring;
using Compze.Tessaging.MicrosoftSql.Wiring;
using Compze.Tessaging.MySql.Wiring;
using Compze.Tessaging.PostgreSql.Wiring;
using Compze.Tessaging.Sqlite.Wiring;
using Compze.Tessaging.Teventive.TeventStore.MicrosoftSql.Wiring;
using Compze.Tessaging.Teventive.TeventStore.MySql.Wiring;
using Compze.Tessaging.Teventive.TeventStore.PostgreSql.Wiring;
using Compze.Tessaging.Teventive.TeventStore.Sqlite.Wiring;
using Compze.TypeIdentifiers.Interning.MicrosoftSql.Wiring;
using Compze.TypeIdentifiers.Interning.MySql.Wiring;
using Compze.TypeIdentifiers.Interning.PostgreSql.Wiring;
using Compze.TypeIdentifiers.Interning.Sqlite.Wiring;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE;
using Compze.Internals.Testing;

namespace Compze.Tessaging.Hosting.Testing.Wiring;

static class TestingComponentRegistrarTestingSqlLayerRegistrar
{
   public static IComponentRegistrar CurrentTestsConfiguredSqlLayer(this IComponentRegistrar register, string connectionStringName) =>
      register.CastTo<TestingComponentRegistrar>()
              .CurrentTestsConfiguredSqlLayer(connectionStringName);

   static IComponentRegistrar CurrentTestsConfiguredSqlLayer(this TestingComponentRegistrar @this, string connectionStringName)
   {
      switch(TestEnv.SqlLayer)
      {
         case SqlLayer.MsSql:
            return @this.MsSqlConnectionPool(connectionStringName)
                        .MsSqlSqlLayerSchemaManager([MsSqlTypeIdInternerRegistrar.SchemaCreationSql, MsSqlDocumentDbRegistrar.SchemaCreationSql, MsSqlTessagingRegistrar.SchemaCreationSql, MsSqlTeventStoreRegistrar.SchemaCreationSql])
                        .MsSqlTypeIdInterner()
                        .MsSqlDocumentDbSqlLayer()
                        .MsSqlTessagingSqlLayer()
                        .MsSqlTeventStoreSqlLayer();
         case SqlLayer.MySql:
            return @this.MySqlConnectionPool(connectionStringName)
                        .MySqlSqlLayerSchemaManager([MySqlTypeIdInternerRegistrar.SchemaCreationSql, MySqlDocumentDbRegistrar.SchemaCreationSql, MySqlTessagingRegistrar.SchemaCreationSql, MySqlTeventStoreRegistrar.SchemaCreationSql])
                        .MySqlTypeIdInterner()
                        .MySqlDocumentDbSqlLayer()
                        .MySqlTessagingSqlLayer()
                        .MySqlTeventStoreSqlLayer();
         case SqlLayer.PgSql:
            return @this.PgSqlConnectionPoolIfNotAlreadyRegistered(connectionStringName)
                        .PgSqlSqlLayerSchemaManager([PgSqlTypeIdInternerRegistrar.SchemaCreationSql, PgSqlDocumentDbRegistrar.SchemaCreationSql, PgSqlTessagingRegistrar.SchemaCreationSql, PgSqlTeventStoreRegistrar.SchemaCreationSql])
                        .PgSqlTypeIdInterner()
                        .PgSqlDocumentDbSqlLayer()
                        .PgSqlTessagingSqlLayer()
                        .PgSqlTeventStoreSqlLayer();
         case SqlLayer.Sqlite:
            return @this.SqliteConnectionPool(connectionStringName)
                        .SqliteSqlLayerSchemaManager([SqliteTypeIdInternerRegistrar.SchemaCreationSql, SqliteDocumentDbRegistrar.SchemaCreationSql, SqliteTessagingRegistrar.SchemaCreationSql, SqliteTeventStoreRegistrar.SchemaCreationSql])
                        .SqliteTypeIdInterner()
                        .SqliteDocumentDbSqlLayer()
                        .SqliteTessagingSqlLayer()
                        .SqliteTeventStoreSqlLayer();
         case SqlLayer.SqliteMemory:
            return @this.SqliteMemoryConnectionPool(connectionStringName)
                        .SqliteSqlLayerSchemaManager([SqliteTypeIdInternerRegistrar.SchemaCreationSql, SqliteDocumentDbRegistrar.SchemaCreationSql, SqliteTessagingRegistrar.SchemaCreationSql, SqliteTeventStoreRegistrar.SchemaCreationSql])
                        .SqliteTypeIdInterner()
                        .SqliteDocumentDbSqlLayer()
                        .SqliteTessagingSqlLayer()
                        .SqliteTeventStoreSqlLayer();
         default:
            throw new ArgumentOutOfRangeException();
      }
   }
}
