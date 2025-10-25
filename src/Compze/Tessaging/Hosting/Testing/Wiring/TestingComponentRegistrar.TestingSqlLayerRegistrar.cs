using System;
using Compze.Core.Wiring.Testing.Internal;
using Compze.Sql.MicrosoftSql;
using Compze.Sql.MicrosoftSql.Private.Tessaging;
using Compze.Sql.MicrosoftSql.Private.TEventStore;
using Compze.Sql.MicrosoftSql.Wiring;
using Compze.Sql.MySql.Private.SystemExtensions;
using Compze.Sql.MySql.Wiring;
using Compze.Sql.PostgreSql;
using Compze.Sql.PostgreSql.DocumentDb.Wiring;
using Compze.Sql.PostgreSql.Tessaging;
using Compze.Sql.PostgreSql.TEventStore;
using Compze.Sql.Sqlite;
using Compze.Sql.Sqlite.DocumentDb.Wiring;
using Compze.Sql.Sqlite.Tessaging;
using Compze.Sql.Sqlite.TEventStore;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;

namespace Compze.Tessaging.Hosting.Testing.Wiring;

public static class TestingComponentRegistrarTestingSqlLayerRegistrar
{
   public static IComponentRegistrar CurrentTestsConfiguredSqlLayer(this IComponentRegistrar register) =>
      register.CurrentTestsConfiguredSqlLayer(Guid.NewGuid().ToString());

   public static IComponentRegistrar CurrentTestsConfiguredSqlLayer(this IComponentRegistrar register, string connectionStringName) =>
      register.CastTo<TestingComponentRegistrar>()
              .CurrentTestsConfiguredSqlLayer(connectionStringName);

   public static IComponentRegistrar CurrentTestsConfiguredSqlLayer(this TestingComponentRegistrar @this, string connectionStringName)
   {
      switch(TestEnv.SqlLayer)
      {
         case SqlLayer.MicrosoftSqlServer:
            return @this.MsSqlConnectionPool(connectionStringName)
                        .MsSqlSqlLayers();
         case SqlLayer.MySql:
            return @this.MySqlConnectionPool(connectionStringName)
                        .MySqlSqlLayers();
         case SqlLayer.PostgreSql:
            return @this.PgSqlConnectionPoolIfNotAlreadyRegistered(connectionStringName)
                        .PgSqlDocumentDb()
                        .PgSqlTeventStore()
                        .PgSqlTessaging();

         case SqlLayer.Sqlite:
            return @this.SqliteConnectionPool(connectionStringName)
                        .SqliteDocumentDb()
                        .SqliteTeventStore()
                        .SqliteTessaging();
         case SqlLayer.SqliteMemory:
            return @this.SqliteMemoryConnectionPool(connectionStringName)
                        .SqliteDocumentDb()
                        .SqliteTeventStore()
                        .SqliteTessaging();
         default:
            throw new ArgumentOutOfRangeException();
      }
   }
}
